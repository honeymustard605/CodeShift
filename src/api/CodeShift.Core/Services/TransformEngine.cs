using CodeShift.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Services;

public class TransformEngine
{
    private readonly ILogger<TransformEngine> _logger;

    public TransformEngine(ILogger<TransformEngine> logger)
    {
        _logger = logger;
    }

    public async Task<TransformResult> PreviewAsync(
        string filePath, string targetFramework, CancellationToken cancellationToken)
    {
        var result = await TransformCoreAsync(filePath, targetFramework, apply: false, cancellationToken);
        return result;
    }

    public async Task<TransformResult> TransformAsync(
        string filePath, string targetFramework, CancellationToken cancellationToken)
    {
        var result = await TransformCoreAsync(filePath, targetFramework, apply: true, cancellationToken);
        if (result.Success)
            await File.WriteAllTextAsync(filePath, result.TransformedSource, cancellationToken);
        return result;
    }

    private async Task<TransformResult> TransformCoreAsync(
        string filePath, string targetFramework, bool apply, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            return Fail(filePath, targetFramework, $"File not found: {filePath}");

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext != ".cs")
            return Fail(filePath, targetFramework, "Only C# transformation is currently supported.");

        var source = await File.ReadAllTextAsync(filePath, cancellationToken);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = await tree.GetRootAsync(cancellationToken);

        var appliedRules = new List<string>();
        var warnings = new List<string>();

        // Rule: replace HttpContext.Current with IHttpContextAccessor
        if (source.Contains("HttpContext.Current"))
        {
            root = new HttpContextCurrentRewriter().Visit(root);
            appliedRules.Add("HttpContext.Current → IHttpContextAccessor");
        }

        // Rule: replace Thread.Sleep with Task.Delay
        if (source.Contains("Thread.Sleep"))
        {
            root = new ThreadSleepRewriter().Visit(root);
            appliedRules.Add("Thread.Sleep → await Task.Delay");
            warnings.Add("Thread.Sleep replacement requires the enclosing method to be async.");
        }

        _logger.LogInformation("Transformed {File}: {RuleCount} rules applied", filePath, appliedRules.Count);

        return new TransformResult(
            FilePath: filePath,
            OriginalSource: source,
            TransformedSource: root.ToFullString(),
            TargetFramework: targetFramework,
            AppliedRules: appliedRules,
            Warnings: warnings,
            Success: true);
    }

    private static TransformResult Fail(string filePath, string targetFramework, string error) =>
        new(filePath, string.Empty, string.Empty, targetFramework, [], [error], false);
}

internal class HttpContextCurrentRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.ToString() == "HttpContext.Current")
            return SyntaxFactory.ParseExpression("_httpContextAccessor.HttpContext")
                .WithTriviaFrom(node);
        return base.VisitMemberAccessExpression(node);
    }
}

internal class ThreadSleepRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression.ToString() == "Thread.Sleep")
        {
            var args = node.ArgumentList.ToString();
            return SyntaxFactory.ParseExpression($"await Task.Delay{args}")
                .WithTriviaFrom(node);
        }
        return base.VisitInvocationExpression(node);
    }
}
