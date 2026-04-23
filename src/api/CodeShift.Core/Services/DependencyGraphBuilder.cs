using CodeShift.Core.Models;
using Microsoft.Extensions.Logging;

namespace CodeShift.Core.Services;

public class DependencyGraphBuilder
{
    private readonly ILogger<DependencyGraphBuilder> _logger;

    public DependencyGraphBuilder(ILogger<DependencyGraphBuilder> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds an adjacency-list graph from the raw edges in <paramref name="result"/>.
    /// Returns nodes (unique names) and deduplicated edges.
    /// </summary>
    public (IReadOnlyList<string> Nodes, IReadOnlyList<DependencyEdge> Edges) Build(AnalysisResult result)
    {
        var nodes = result.Dependencies
            .SelectMany(e => new[] { e.Source, e.Target })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToList();

        // Deduplicate same-direction edges; keep first occurrence
        var seen = new HashSet<(string, string)>();
        var edges = new List<DependencyEdge>();
        foreach (var edge in result.Dependencies)
        {
            if (seen.Add((edge.Source, edge.Target)))
                edges.Add(edge);
        }

        _logger.LogDebug("Graph built: {NodeCount} nodes, {EdgeCount} edges", nodes.Count, edges.Count);
        return (nodes, edges);
    }
}
