using CodeShift.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Analyzers;

public class VbNetAnalyzer : ICodebaseAnalyzer
{
    private readonly ILogger<VbNetAnalyzer> _logger;

    public VbNetAnalyzer(ILogger<VbNetAnalyzer> logger)
    {
        _logger = logger;
    }

    public bool CanAnalyze(string rootPath) =>
        Directory.GetFiles(rootPath, "*.vb", SearchOption.AllDirectories).Length > 0;

    public async Task<AnalysisResult> AnalyzeAsync(string rootPath, CancellationToken cancellationToken)
    {
        var vbFiles = Directory.GetFiles(rootPath, "*.vb", SearchOption.AllDirectories);
        _logger.LogInformation("Analyzing {Count} VB.NET files in {Root}", vbFiles.Length, rootPath);

        var risks = new List<RiskFlag>();
        var edges = new List<DependencyEdge>();
        int totalLoc = 0;

        foreach (var file in vbFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lines = await File.ReadAllLinesAsync(file, cancellationToken);
            totalLoc += lines.Length;

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();

                // Collect Imports as edges
                if (trimmed.StartsWith("Imports ", StringComparison.OrdinalIgnoreCase))
                {
                    var ns = trimmed["Imports ".Length..].Trim();
                    edges.Add(new DependencyEdge(
                        Source: Path.GetFileNameWithoutExtension(file),
                        Target: ns,
                        Kind: "imports"));
                }

                // Risk flags
                if (trimmed.Contains("Microsoft.VisualBasic.PowerPacks"))
                    risks.Add(new RiskFlag("VBPowerPacks", file, RiskLevel.High, "PowerPacks has no .NET Core equivalent."));

                if (trimmed.Contains("CreateObject("))
                    risks.Add(new RiskFlag("LateBinding", file, RiskLevel.Medium, "Late-bound COM CreateObject — review for COM interop."));
            }
        }

        var projects = new List<DetectedProject>
        {
            new(
                Name: Path.GetFileName(rootPath),
                Language: "VB.NET",
                FileCount: vbFiles.Length,
                RootPath: rootPath,
                TargetFramework: "net4x")
        };

        return new AnalysisResult(
            Language: "VB.NET",
            Projects: projects,
            Dependencies: edges,
            Risks: risks,
            TotalFiles: vbFiles.Length,
            TotalLoc: totalLoc,
            AnalyzedAt: DateTime.UtcNow);
    }
}
