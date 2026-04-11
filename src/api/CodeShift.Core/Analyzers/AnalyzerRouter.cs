using CodeShift.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Analyzers;

/// <summary>
/// Inspects the root path to detect the dominant language/technology, then
/// dispatches to the appropriate concrete analyzer.
/// </summary>
public class AnalyzerRouter : ICodebaseAnalyzer
{
    private readonly CSharpAnalyzer _csharp;
    private readonly VbNetAnalyzer _vbnet;
    private readonly Vb6Analyzer _vb6;
    private readonly ILogger<AnalyzerRouter> _logger;

    public AnalyzerRouter(
        CSharpAnalyzer csharp,
        VbNetAnalyzer vbnet,
        Vb6Analyzer vb6,
        ILogger<AnalyzerRouter> logger)
    {
        _csharp = csharp;
        _vbnet = vbnet;
        _vb6 = vb6;
        _logger = logger;
    }

    public bool CanAnalyze(string rootPath) => true; // router handles all

    public async Task<AnalysisResult> AnalyzeAsync(string rootPath, CancellationToken cancellationToken)
    {
        var detected = DetectLanguage(rootPath);
        _logger.LogInformation("Detected language {Language} for {RootPath}", detected, rootPath);

        return detected switch
        {
            DetectedLanguage.CSharp => await _csharp.AnalyzeAsync(rootPath, cancellationToken),
            DetectedLanguage.VbNet  => await _vbnet.AnalyzeAsync(rootPath, cancellationToken),
            DetectedLanguage.Vb6    => await _vb6.AnalyzeAsync(rootPath, cancellationToken),
            _ => throw new NotSupportedException($"No analyzer for language: {detected}")
        };
    }

    private static DetectedLanguage DetectLanguage(string rootPath)
    {
        var csFiles  = Directory.GetFiles(rootPath, "*.cs",  SearchOption.AllDirectories);
        var vbnFiles = Directory.GetFiles(rootPath, "*.vb",  SearchOption.AllDirectories);
        var vb6Files = Directory.GetFiles(rootPath, "*.bas", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(rootPath, "*.cls", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles(rootPath, "*.frm", SearchOption.AllDirectories))
            .ToArray();

        if (vb6Files.Length > vbnFiles.Length && vb6Files.Length > csFiles.Length)
            return DetectedLanguage.Vb6;
        if (vbnFiles.Length > csFiles.Length)
            return DetectedLanguage.VbNet;
        return DetectedLanguage.CSharp;
    }

    private enum DetectedLanguage { CSharp, VbNet, Vb6 }
}
