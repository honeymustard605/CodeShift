using CodeShift.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Services;

public class VbNetTransformEngine
{
    private readonly ILogger<VbNetTransformEngine> _logger;

    public VbNetTransformEngine(ILogger<VbNetTransformEngine> logger)
    {
        _logger = logger;
    }

    public async Task<TransformResult> PreviewAsync(string filePath, string targetFramework, CancellationToken cancellationToken) =>
        await TransformCoreAsync(filePath, targetFramework, apply: false, cancellationToken);

    public async Task<TransformResult> TransformAsync(string filePath, string targetFramework, CancellationToken cancellationToken)
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

        if (!filePath.EndsWith(".vb", StringComparison.OrdinalIgnoreCase))
            return Fail(filePath, targetFramework, "VbNetTransformEngine only handles .vb files.");

        var source = await File.ReadAllTextAsync(filePath, cancellationToken);
        var tree = VisualBasicSyntaxTree.ParseText(source);
        var root = await tree.GetRootAsync(cancellationToken);

        var appliedRules = new List<string>();
        var warnings = new List<string>();

        // Rule: System.Data.SqlClient → Microsoft.Data.SqlClient
        if (source.Contains("System.Data.SqlClient"))
        {
            root = new SqlClientNamespaceRewriter().Visit(root);
            appliedRules.Add("System.Data.SqlClient → Microsoft.Data.SqlClient");
            warnings.Add("Add 'Microsoft.Data.SqlClient' NuGet package to your project.");
        }

        // Rule: Imports Microsoft.VisualBasic → remove (not needed in .NET 8)
        if (source.Contains("Imports Microsoft.VisualBasic"))
        {
            root = new RemoveImportsRewriter("Microsoft.VisualBasic").Visit(root);
            appliedRules.Add("Imports Microsoft.VisualBasic → removed");
        }

        // Rule: CreateObject — flag as COM dependency (no rewrite, just warning)
        if (source.Contains("CreateObject("))
        {
            warnings.Add("CreateObject detected — replace COM dependencies with .NET equivalents.");
            appliedRules.Add("CreateObject → flagged for manual replacement");
        }

        _logger.LogInformation("VB.NET transform applied to {File}: {Count} rules", filePath, appliedRules.Count);

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

internal class SqlClientNamespaceRewriter : VisualBasicSyntaxRewriter
{
    public override SyntaxNode? VisitImportsStatement(ImportsStatementSyntax node)
    {
        var newClauses = node.ImportsClauses.Select(clause =>
        {
            if (clause is SimpleImportsClauseSyntax simple &&
                simple.Name.ToString().Contains("System.Data.SqlClient"))
            {
                var newName = SyntaxFactory.ParseName("Microsoft.Data.SqlClient")
                    .WithTriviaFrom(simple.Name);
                return (ImportsClauseSyntax)simple.WithName((NameSyntax)newName);
            }
            return clause;
        });

        return node.WithImportsClauses(SyntaxFactory.SeparatedList(newClauses));
    }

    public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
    {
        if (node.ToString().StartsWith("System.Data.SqlClient", StringComparison.Ordinal))
        {
            var replaced = node.ToString().Replace("System.Data.SqlClient", "Microsoft.Data.SqlClient");
            return SyntaxFactory.ParseName(replaced).WithTriviaFrom(node);
        }
        return base.VisitQualifiedName(node);
    }
}

internal class RemoveImportsRewriter : VisualBasicSyntaxRewriter
{
    private readonly string _namespace;

    public RemoveImportsRewriter(string ns) => _namespace = ns;

    public override SyntaxNode? VisitImportsStatement(ImportsStatementSyntax node)
    {
        var remaining = node.ImportsClauses.Where(c =>
            c is not SimpleImportsClauseSyntax s ||
            !s.Name.ToString().Contains(_namespace)).ToList();

        if (remaining.Count == 0) return null; // remove entire Imports line
        return node.WithImportsClauses(SyntaxFactory.SeparatedList(remaining));
    }
}
