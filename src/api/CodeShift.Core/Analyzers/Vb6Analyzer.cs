using CodeShift.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Analyzers;

public class Vb6Analyzer : ICodebaseAnalyzer
{
    private static readonly string[] Vb6Extensions = ["*.bas", "*.cls", "*.frm", "*.vbp"];
    private readonly ILogger<Vb6Analyzer> _logger;

    public Vb6Analyzer(ILogger<Vb6Analyzer> logger)
    {
        _logger = logger;
    }

    public bool CanAnalyze(string rootPath) =>
        Vb6Extensions.Any(ext =>
            Directory.GetFiles(rootPath, ext, SearchOption.AllDirectories).Length > 0);

    public async Task<AnalysisResult> AnalyzeAsync(string rootPath, CancellationToken cancellationToken)
    {
        var allFiles = Vb6Extensions
            .SelectMany(ext => Directory.GetFiles(rootPath, ext, SearchOption.AllDirectories))
            .ToArray();

        _logger.LogInformation("Analyzing {Count} VB6 files in {Root}", allFiles.Length, rootPath);

        var risks = new List<RiskFlag>();
        var edges = new List<DependencyEdge>();
        int totalLoc = 0;

        foreach (var file in allFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var lines = await File.ReadAllLinesAsync(file, cancellationToken);
            totalLoc += lines.Length;

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();

                // Track object references as dependency edges (handles "Object=" and "Object =")
                if (trimmed.StartsWith("Object", StringComparison.OrdinalIgnoreCase))
                {
                    var eqIndex = trimmed.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        var objectRef = trimmed[(eqIndex + 1)..].Split(';')[0].Trim().Trim('"');
                        edges.Add(new DependencyEdge(
                            Source: Path.GetFileNameWithoutExtension(file),
                            Target: objectRef,
                            Kind: "vb6-ocx"));
                    }
                }

                // High-risk VB6 patterns
                if (trimmed.Contains("CreateObject("))
                    risks.Add(new RiskFlag("COMObject", file, RiskLevel.Critical, "COM object creation — must be replaced with .NET equivalent."));

                if (trimmed.Contains("ADODB") || trimmed.Contains("ADODC"))
                    risks.Add(new RiskFlag("ADO", file, RiskLevel.High, "Legacy ADO data access — migrate to EF Core or Dapper."));

                if (trimmed.Contains("MSFlexGrid") || trimmed.Contains("DataGrid"))
                    risks.Add(new RiskFlag("LegacyGrid", file, RiskLevel.Medium, "VB6 grid control — no direct WinForms/.NET equivalent."));
            }
        }

        // Always flag VB6 as critical migration risk
        risks.Insert(0, new RiskFlag(
            "VB6Runtime",
            rootPath,
            RiskLevel.Critical,
            "VB6 requires the Visual Basic 6.0 runtime — no support on modern Windows Server."));

        var projects = new List<DetectedProject>
        {
            new(
                Name: Path.GetFileName(rootPath),
                Language: "VB6",
                FileCount: allFiles.Length,
                RootPath: rootPath,
                TargetFramework: "vb6")
        };

        return new AnalysisResult(
            Language: "VB6",
            Projects: projects,
            Dependencies: edges,
            Risks: risks,
            TotalFiles: allFiles.Length,
            TotalLoc: totalLoc,
            AnalyzedAt: DateTime.UtcNow);
    }
}
