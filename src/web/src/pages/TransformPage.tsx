import { useState } from "react";
import { useParams, Link } from "react-router-dom";
import { previewTransform, applyTransform } from "../api/client";
import type { TransformResult } from "../types";
import DiffViewer from "../components/DiffViewer";

export default function TransformPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [filePath, setFilePath] = useState("");
  const [targetFramework, setTargetFramework] = useState("net8.0");
  const [result, setResult] = useState<TransformResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [applying, setApplying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [applied, setApplied] = useState(false);

  const handlePreview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!projectId) return;
    setLoading(true);
    setError(null);
    setApplied(false);
    try {
      const r = await previewTransform(projectId, filePath, targetFramework);
      setResult(r);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Preview failed");
    } finally {
      setLoading(false);
    }
  };

  const handleApply = async () => {
    if (!projectId) return;
    setApplying(true);
    setError(null);
    try {
      await applyTransform(projectId, filePath, targetFramework);
      setApplied(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Apply failed");
    } finally {
      setApplying(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto p-8 flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Transform</h1>
        <Link to={`/dashboard/${projectId}`} className="text-brand-500 hover:underline text-sm">
          ← Dashboard
        </Link>
      </div>

      <form onSubmit={handlePreview} className="rounded-xl bg-gray-900 border border-gray-700 p-6 flex flex-col gap-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">File Path</label>
            <input
              type="text"
              value={filePath}
              onChange={(e) => setFilePath(e.target.value)}
              placeholder="/path/to/File.cs"
              required
              className="w-full rounded-lg bg-gray-800 border border-gray-700 px-4 py-2 text-gray-100 focus:outline-none focus:ring-2 focus:ring-brand-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">Target Framework</label>
            <select
              value={targetFramework}
              onChange={(e) => setTargetFramework(e.target.value)}
              className="w-full rounded-lg bg-gray-800 border border-gray-700 px-4 py-2 text-gray-100 focus:outline-none focus:ring-2 focus:ring-brand-500"
            >
              <option value="net8.0">.NET 8</option>
              <option value="net6.0">.NET 6</option>
            </select>
          </div>
        </div>

        <button
          type="submit"
          disabled={loading}
          className="self-start rounded-lg bg-brand-600 hover:bg-brand-700 disabled:opacity-50 px-6 py-2 font-semibold transition-colors"
        >
          {loading ? "Previewing…" : "Preview Transform"}
        </button>
      </form>

      {error && <p className="text-red-400 text-sm">{error}</p>}

      {result && (
        <div className="flex flex-col gap-4">
          <DiffViewer result={result} />

          {!applied ? (
            <button
              onClick={handleApply}
              disabled={applying || !result.success}
              className="self-start rounded-lg bg-green-700 hover:bg-green-600 disabled:opacity-50 px-6 py-2 font-semibold transition-colors"
            >
              {applying ? "Applying…" : "Apply Transform"}
            </button>
          ) : (
            <p className="text-green-400 font-medium">Transform applied successfully.</p>
          )}
        </div>
      )}
    </div>
  );
}
