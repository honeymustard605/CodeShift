namespace CodeShift.Core.Models;

public record TransformResult(
    string FilePath,
    string OriginalSource,
    string TransformedSource,
    string TargetFramework,
    List<string> AppliedRules,
    List<string> Warnings,
    bool Success);
