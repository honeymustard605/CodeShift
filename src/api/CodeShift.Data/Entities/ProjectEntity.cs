using System.ComponentModel.DataAnnotations;

namespace CodeShift.Data.Entities;

public class ProjectEntity
{
    public Guid Id { get; set; }

    [MaxLength(256)]
    public required string Name { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(64)]
    public required string Status { get; set; }

    // Stores the serialized AnalysisResult JSON
    public string? AnalysisJson { get; set; }

    // Stores the serialized MigrationRoadmap JSON
    public string? RoadmapJson { get; set; }
}
