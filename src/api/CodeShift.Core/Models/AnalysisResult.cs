namespace CodeShift.Core.Models;

public record AnalysisResult(
    string Language,
    List<DetectedProject> Projects,
    List<DependencyEdge> Dependencies,
    List<RiskFlag> Risks,
    int TotalFiles,
    int TotalLoc,
    DateTime AnalyzedAt);
