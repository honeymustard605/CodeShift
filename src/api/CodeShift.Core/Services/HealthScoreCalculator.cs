using CodeShift.Core.Models;

namespace CodeShift.Core.Services;

public class HealthScoreCalculator
{
    // Score is 0–100 (100 = already modern, 0 = extremely risky/legacy)
    public int Calculate(AnalysisResult result)
    {
        int score = 100;

        // Penalise by risk level
        foreach (var risk in result.Risks)
        {
            score -= risk.Level switch
            {
                RiskLevel.Critical => 20,
                RiskLevel.High     => 10,
                RiskLevel.Medium   =>  5,
                RiskLevel.Low      =>  2,
                _ => 0
            };
        }

        // Penalise for VB6 / legacy framework
        if (result.Language == "VB6") score -= 30;
        if (result.Projects.Any(p => p.TargetFramework == "net4x")) score -= 10;

        // Reward for already being on .NET 8
        if (result.Projects.All(p => p.TargetFramework == "net8.0")) score += 10;

        return Math.Clamp(score, 0, 100);
    }
}
