import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { getAnalysis } from "../api/client";
import type { AnalysisResult } from "../types";
import HealthScore from "../components/HealthScore";
import LanguageBreakdown from "../components/LanguageBreakdown";
import TechBreakdown from "../components/TechBreakdown";
import RiskFlags from "../components/RiskFlags";

// Client-side health score calculation (mirrors backend logic)
function calcHealthScore(result: AnalysisResult): number {
  let score = 100;
  for (const risk of result.risks) {
    score -= risk.level === "Critical" ? 20 : risk.level === "High" ? 10 : risk.level === "Medium" ? 5 : 2;
  }
  if (result.language === "VB6") score -= 30;
  if (result.projects.some((p) => p.targetFramework === "net4x")) score -= 10;
  if (result.projects.every((p) => p.targetFramework === "net8.0")) score += 10;
  return Math.max(0, Math.min(100, score));
}

export default function DashboardPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [analysis, setAnalysis] = useState<AnalysisResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!projectId) return;
    getAnalysis(projectId)
      .then(setAnalysis)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [projectId]);

  if (loading) return <div className="flex items-center justify-center min-h-screen text-gray-400">Loading…</div>;
  if (error || !analysis) return <div className="p-8 text-red-400">{error ?? "No analysis found"}</div>;

  const score = calcHealthScore(analysis);

  return (
    <div className="max-w-5xl mx-auto p-8 flex flex-col gap-8">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Analysis Dashboard</h1>
        <nav className="flex gap-3 text-sm">
          <Link to={`/graph/${projectId}`} className="text-brand-500 hover:underline">Dependency Graph</Link>
          <Link to={`/roadmap/${projectId}`} className="text-brand-500 hover:underline">Roadmap</Link>
          <Link to={`/transform/${projectId}`} className="text-brand-500 hover:underline">Transform</Link>
          <Link to="/" className="text-gray-400 hover:underline">+ New Analysis</Link>
        </nav>
      </div>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: "Language", value: analysis.language },
          { label: "Files", value: analysis.totalFiles.toLocaleString() },
          { label: "Lines of Code", value: analysis.totalLoc.toLocaleString() },
          { label: "Risk Flags", value: analysis.risks.length },
        ].map((stat) => (
          <div key={stat.label} className="rounded-xl bg-gray-900 border border-gray-700 p-4">
            <p className="text-xs text-gray-500 uppercase tracking-widest mb-1">{stat.label}</p>
            <p className="text-xl font-bold">{stat.value}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="rounded-xl bg-gray-900 border border-gray-700 p-6 flex flex-col items-center gap-2">
          <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wide">Health Score</h2>
          <HealthScore score={score} />
        </div>

        <div className="rounded-xl bg-gray-900 border border-gray-700 p-6">
          <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wide mb-3">Language Breakdown</h2>
          <LanguageBreakdown projects={analysis.projects} />
        </div>

        <div className="rounded-xl bg-gray-900 border border-gray-700 p-6">
          <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wide mb-3">Projects by Framework</h2>
          <TechBreakdown projects={analysis.projects} />
        </div>
      </div>

      <div className="rounded-xl bg-gray-900 border border-gray-700 p-6">
        <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wide mb-4">Risk Flags</h2>
        <RiskFlags risks={analysis.risks} />
      </div>
    </div>
  );
}
