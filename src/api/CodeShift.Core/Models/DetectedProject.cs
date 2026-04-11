namespace CodeShift.Core.Models;

public record DetectedProject(
    string Name,
    string Language,
    int FileCount,
    string RootPath,
    string TargetFramework);
