namespace CodeShift.Core.Models;

public record DependencyEdge(
    string Source,
    string Target,
    string Kind);
