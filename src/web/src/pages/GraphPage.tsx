import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { getAnalysis } from "../api/client";
import type { AnalysisResult, DependencyEdge } from "../types";
import DependencyGraph from "../components/DependencyGraph";

function buildGraph(analysis: AnalysisResult): { nodes: string[]; edges: DependencyEdge[] } {
  const nodeSet = new Set<string>();
  const deduped: DependencyEdge[] = [];
  const seen = new Set<string>();

  for (const edge of analysis.dependencies) {
    nodeSet.add(edge.source);
    nodeSet.add(edge.target);
    const key = `${edge.source}→${edge.target}`;
    if (!seen.has(key)) {
      seen.add(key);
      deduped.push(edge);
    }
  }

  return { nodes: Array.from(nodeSet), edges: deduped };
}

export default function GraphPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [analysis, setAnalysis] = useState<AnalysisResult | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!projectId) return;
    getAnalysis(projectId).then(setAnalysis).finally(() => setLoading(false));
  }, [projectId]);

  if (loading) return <div className="flex items-center justify-center min-h-screen text-gray-400">Loading…</div>;
  if (!analysis) return <div className="p-8 text-red-400">No analysis found</div>;

  const { nodes, edges } = buildGraph(analysis);

  return (
    <div className="max-w-6xl mx-auto p-8 flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Dependency Graph</h1>
        <Link to={`/dashboard/${projectId}`} className="text-brand-500 hover:underline text-sm">
          ← Dashboard
        </Link>
      </div>
      <p className="text-gray-400 text-sm">{nodes.length} nodes · {edges.length} edges</p>
      <DependencyGraph nodes={nodes} edges={edges} />
    </div>
  );
}
