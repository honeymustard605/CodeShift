namespace CodeShift.Core.Models;

public record RiskFlag(
    string Category,
    string FilePath,
    RiskLevel Level,
    string Description);

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
