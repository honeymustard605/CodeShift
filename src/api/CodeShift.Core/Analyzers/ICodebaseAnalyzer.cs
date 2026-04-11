using CodeShift.Core.Models;

namespace CodeShift.Core.Analyzers;

public interface ICodebaseAnalyzer
{
    /// <summary>Analyze a codebase rooted at <paramref name="rootPath"/>.</summary>
    Task<AnalysisResult> AnalyzeAsync(string rootPath, CancellationToken cancellationToken);

    /// <summary>Returns true when this analyzer can handle the given root path.</summary>
    bool CanAnalyze(string rootPath);
}
