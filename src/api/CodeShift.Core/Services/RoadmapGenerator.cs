using CodeShift.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Services;

public class RoadmapGenerator
{
    private readonly HealthScoreCalculator _health;
    private readonly ILogger<RoadmapGenerator> _logger;

    public RoadmapGenerator(HealthScoreCalculator health, ILogger<RoadmapGenerator> logger)
    {
        _health = health;
        _logger = logger;
    }

    public Task<MigrationRoadmap> GenerateAsync(AnalysisResult analysis, CancellationToken cancellationToken)
    {
        var score = _health.Calculate(analysis);
        var phases = BuildPhases(analysis, score);
        var totalWeeks = phases.Sum(p => p.EstimatedWeeks);

        _logger.LogInformation("Generated roadmap: {PhaseCount} phases, ~{Weeks} weeks", phases.Count, totalWeeks);

        return Task.FromResult(new MigrationRoadmap(
            ProjectId: Guid.NewGuid(),
            Phases: phases,
            EstimatedWeeks: totalWeeks,
            TargetFramework: "net8.0",
            GeneratedAt: DateTime.UtcNow));
    }

    private static List<RoadmapPhase> BuildPhases(AnalysisResult analysis, int healthScore)
    {
        var phases = new List<RoadmapPhase>();

        // Phase 1 — Inventory & Baseline
        phases.Add(new RoadmapPhase(
            Order: 1,
            Name: "Inventory & Baseline",
            Description: "Establish test coverage baseline, document current architecture.",
            Tasks:
            [
                new("Add characterization tests", "Capture current behavior before any changes.", RiskLevel.Medium, null),
                new("Document external integrations", "Map all COM, DB, and service dependencies.", RiskLevel.High, null)
            ],
            EstimatedWeeks: 2));

        // Phase 2 — Lift & Shift (framework upgrade)
        if (analysis.Language == "VB6")
        {
            phases.Add(new RoadmapPhase(
                Order: 2,
                Name: "VB6 → VB.NET Conversion",
                Description: "Use upgrade wizard / manual rewrite to get to VB.NET as intermediate step.",
                Tasks:
                [
                    new("Run VB6 Upgrade Wizard", "Auto-convert .frm/.cls/.bas to VB.NET.", RiskLevel.High, null),
                    new("Fix upgrade wizard TODO comments", "Wizard leaves TODO markers for unsupported constructs.", RiskLevel.Critical, null)
                ],
                EstimatedWeeks: 4));
        }

        // Phase 3 — Framework-specific risk remediation
        var criticalRisks = analysis.Risks.Where(r => r.Level >= RiskLevel.High).ToList();
        if (criticalRisks.Count > 0)
        {
            phases.Add(new RoadmapPhase(
                Order: phases.Count + 1,
                Name: "Risk Remediation",
                Description: "Address high/critical risk items identified during analysis.",
                Tasks: criticalRisks
                    .Take(10)
                    .Select(r => new RoadmapTask(
                        Title: $"Resolve {r.Category}",
                        Description: r.Description,
                        Complexity: r.Level,
                        FileReference: r.FilePath))
                    .ToList(),
                EstimatedWeeks: (int)Math.Ceiling(criticalRisks.Count / 3.0)));
        }

        // Phase 4 — .NET 8 migration
        phases.Add(new RoadmapPhase(
            Order: phases.Count + 1,
            Name: "Target Framework Migration",
            Description: "Upgrade project files and resolve breaking changes to target net8.0.",
            Tasks:
            [
                new("Update .csproj TargetFramework", "Change TargetFramework to net8.0.", RiskLevel.Low, null),
                new("Replace deprecated APIs", "Address HttpContext, Thread, and other BCL changes.", RiskLevel.Medium, null),
                new("Migrate configuration", "Move Web.config / App.config to appsettings.json.", RiskLevel.Medium, null)
            ],
            EstimatedWeeks: 3));

        // Phase 5 — Validation
        phases.Add(new RoadmapPhase(
            Order: phases.Count + 1,
            Name: "Validation & Deployment",
            Description: "Run full test suite, deploy to staging, sign off.",
            Tasks:
            [
                new("Run integration test suite", "Verify end-to-end behavior matches pre-migration baseline.", RiskLevel.Medium, null),
                new("Performance benchmark", "Compare latency and throughput vs legacy.", RiskLevel.Low, null),
                new("Production cut-over", "Blue/green deployment with rollback plan.", RiskLevel.High, null)
            ],
            EstimatedWeeks: 2));

        return phases;
    }
}
