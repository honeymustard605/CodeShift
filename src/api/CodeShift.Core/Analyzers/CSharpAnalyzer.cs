using CodeShift.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Analyzers;

public class CSharpAnalyzer : ICodebaseAnalyzer
{
    private readonly ILogger<CSharpAnalyzer> _logger;

    public CSharpAnalyzer(ILogger<CSharpAnalyzer> logger)
    {
        _logger = logger;
    }

    public bool CanAnalyze(string rootPath) =>
        Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories).Length > 0;

    public async Task<AnalysisResult> AnalyzeAsync(string rootPath, CancellationToken cancellationToken)
    {
        var csFiles = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);
        _logger.LogInformation("Analyzing {Count} C# files in {Root}", csFiles.Length, rootPath);

        var projects = new List<DetectedProject>();
        var edges = new List<DependencyEdge>();
        var risks = new List<RiskFlag>();
        int totalLoc = 0;

        foreach (var file in csFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = await File.ReadAllTextAsync(file, cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = await tree.GetRootAsync(cancellationToken);

            totalLoc += source.Split('\n').Length;

            // Detect using directives for edge mapping
            var usings = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => n is not null)
                .ToList();

            foreach (var ns in usings)
            {
                edges.Add(new DependencyEdge(
                    Source: Path.GetFileNameWithoutExtension(file),
                    Target: ns!,
                    Kind: "using"));
            }

            // Flag legacy patterns
            if (source.Contains("System.Web.UI") || source.Contains("WebForms"))
                risks.Add(new RiskFlag("WebForms", file, RiskLevel.High, "WebForms page detected — no direct .NET Core equivalent."));

            if (source.Contains("System.ServiceModel"))
                risks.Add(new RiskFlag("WCF", file, RiskLevel.High, "WCF service reference — consider gRPC or minimal API."));
        }

        // Group into pseudo-projects by directory depth 1
        var groupedByDir = csFiles
            .GroupBy(f => Path.GetFileName(Path.GetDirectoryName(f) ?? rootPath))
            .Select(g => new DetectedProject(
                Name: g.Key,
                Language: "C#",
                FileCount: g.Count(),
                RootPath: Path.GetDirectoryName(g.First()) ?? rootPath,
                TargetFramework: DetectTargetFramework(g.Key, rootPath)))
            .ToList();

        projects.AddRange(groupedByDir);

        return new AnalysisResult(
            Language: "C#",
            Projects: projects,
            Dependencies: edges,
            Risks: risks,
            TotalFiles: csFiles.Length,
            TotalLoc: totalLoc,
            AnalyzedAt: DateTime.UtcNow);
    }

    private static string DetectTargetFramework(string projectDir, string rootPath)
    {
        var csprojFiles = Directory.GetFiles(
            Path.Combine(rootPath, projectDir),
            "*.csproj",
            SearchOption.TopDirectoryOnly);

        if (csprojFiles.Length == 0) return "unknown";

        var content = File.ReadAllText(csprojFiles[0]);
        if (content.Contains("net8")) return "net8.0";
        if (content.Contains("net6")) return "net6.0";
        if (content.Contains("netcoreapp")) return "netcoreapp";
        if (content.Contains("net4")) return "net4x";
        return "unknown";
    }
}
