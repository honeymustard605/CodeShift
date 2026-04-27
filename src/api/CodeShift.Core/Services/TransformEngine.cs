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

        // Rule: new WebClient() → HttpClient
        if (source.Contains("WebClient"))
        {
            root = new WebClientRewriter().Visit(root);
            appliedRules.Add("new WebClient() → HttpClient");
            warnings.Add("Inject IHttpClientFactory via DI rather than instantiating HttpClient directly.");
        }

        // Rule: new Thread( → Task.Run(
        if (source.Contains("new Thread("))
        {
            root = new NewThreadRewriter().Visit(root);
            appliedRules.Add("new Thread() → Task.Run()");
            warnings.Add("Task.Run replacement assumes the thread body has no thread-affine state.");
        }

        // Rule: ConfigurationManager.AppSettings → IConfiguration
        if (source.Contains("ConfigurationManager.AppSettings"))
        {
            root = new ConfigurationManagerRewriter().Visit(root);
            appliedRules.Add("ConfigurationManager.AppSettings → IConfiguration");
            warnings.Add("Inject IConfiguration via constructor and update using directives.");
        }

        // Rule: Newtonsoft.Json → System.Text.Json
        if (source.Contains("JsonConvert.SerializeObject") || source.Contains("JsonConvert.DeserializeObject"))
        {
            root = new NewtonsoftJsonRewriter().Visit(root);
            appliedRules.Add("JsonConvert → System.Text.Json");
            warnings.Add("System.Text.Json has stricter defaults than Newtonsoft — verify serialization behaviour.");
        }

        // Rule: Response.Write( → HttpContext.Response.WriteAsync(
        if (source.Contains("Response.Write("))
        {
            root = new ResponseWriteRewriter().Visit(root);
            appliedRules.Add("Response.Write → HttpContext.Response.WriteAsync");
            warnings.Add("Response.WriteAsync is async — ensure the enclosing method is async.");
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

internal class WebClientRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (node.Type.ToString() == "WebClient")
            return SyntaxFactory.ParseExpression("new HttpClient()")
                .WithTriviaFrom(node);
        return base.VisitObjectCreationExpression(node);
    }
}

internal class NewThreadRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        if (node.Type.ToString() == "Thread" && node.ArgumentList is not null)
        {
            var args = node.ArgumentList.Arguments.ToString();
            return SyntaxFactory.ParseExpression($"Task.Run({args})")
                .WithTriviaFrom(node);
        }
        return base.VisitObjectCreationExpression(node);
    }
}

internal class ConfigurationManagerRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.ToString().StartsWith("ConfigurationManager.AppSettings"))
            return SyntaxFactory.ParseExpression("_configuration")
                .WithTriviaFrom(node);
        return base.VisitMemberAccessExpression(node);
    }
}

internal class NewtonsoftJsonRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var expr = node.Expression.ToString();

        if (expr == "JsonConvert.SerializeObject")
        {
            var args = node.ArgumentList.ToString();
            return SyntaxFactory.ParseExpression($"JsonSerializer.Serialize{args}")
                .WithTriviaFrom(node);
        }

        if (expr == "JsonConvert.DeserializeObject" || expr.StartsWith("JsonConvert.DeserializeObject<"))
        {
            var args = node.ArgumentList.Arguments.ToString();
            var generic = node.Expression.ToString().Contains("<")
                ? node.Expression.ToString().Replace("JsonConvert.DeserializeObject", "JsonSerializer.Deserialize")
                : "JsonSerializer.Deserialize";
            return SyntaxFactory.ParseExpression($"{generic}({args})")
                .WithTriviaFrom(node);
        }

        return base.VisitInvocationExpression(node);
    }
}

internal class ResponseWriteRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression.ToString() == "Response.Write")
        {
            var args = node.ArgumentList.ToString();
            return SyntaxFactory.ParseExpression($"await HttpContext.Response.WriteAsync{args}")
                .WithTriviaFrom(node);
        }
        return base.VisitInvocationExpression(node);
    }
}
