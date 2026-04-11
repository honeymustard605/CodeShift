namespace CodeShift.Core.Models;

public record MigrationRoadmap(
    Guid ProjectId,
    List<RoadmapPhase> Phases,
    int EstimatedWeeks,
    string TargetFramework,
    DateTime GeneratedAt);

public record RoadmapPhase(
    int Order,
    string Name,
    string Description,
    List<RoadmapTask> Tasks,
    int EstimatedWeeks);

public record RoadmapTask(
    string Title,
    string Description,
    RiskLevel Complexity,
    string? FileReference);
