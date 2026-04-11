import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { getRoadmap, generateRoadmap } from "../api/client";
import type { MigrationRoadmap } from "../types";
import RoadmapTimeline from "../components/RoadmapTimeline";

export default function RoadmapPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [roadmap, setRoadmap] = useState<MigrationRoadmap | null>(null);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!projectId) return;
    getRoadmap(projectId)
      .then(setRoadmap)
      .catch(() => setRoadmap(null))
      .finally(() => setLoading(false));
  }, [projectId]);

  const handleGenerate = async () => {
    if (!projectId) return;
    setGenerating(true);
    setError(null);
    try {
      const result = await generateRoadmap(projectId);
      setRoadmap(result);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Generation failed");
    } finally {
      setGenerating(false);
    }
  };

  if (loading) return <div className="flex items-center justify-center min-h-screen text-gray-400">Loading…</div>;

  return (
    <div className="max-w-3xl mx-auto p-8 flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Migration Roadmap</h1>
        <Link to={`/dashboard/${projectId}`} className="text-brand-500 hover:underline text-sm">
          ← Dashboard
        </Link>
      </div>

      {!roadmap && (
        <div className="rounded-xl bg-gray-900 border border-gray-700 p-8 text-center">
          <p className="text-gray-400 mb-4">No roadmap generated yet.</p>
          <button
            onClick={handleGenerate}
            disabled={generating}
            className="rounded-lg bg-brand-600 hover:bg-brand-700 disabled:opacity-50 px-6 py-2 font-semibold transition-colors"
          >
            {generating ? "Generating…" : "Generate Roadmap"}
          </button>
          {error && <p className="text-red-400 text-sm mt-3">{error}</p>}
        </div>
      )}

      {roadmap && (
        <>
          <div className="flex justify-end">
            <button
              onClick={handleGenerate}
              disabled={generating}
              className="text-sm text-gray-400 hover:text-white transition-colors disabled:opacity-50"
            >
              {generating ? "Regenerating…" : "Regenerate"}
            </button>
          </div>
          <RoadmapTimeline roadmap={roadmap} />
        </>
      )}
    </div>
  );
}
