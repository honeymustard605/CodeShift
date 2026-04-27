import { useState, useEffect } from "react";
import { useParams, Link } from "react-router-dom";
import axios from "axios";
import { getAnalysis, previewTransform, applyTransform, applyContent, downloadFile, modernizeFile } from "../api/client";
import type { AnalysisResult, TransformResult } from "../types";
import DiffViewer from "../components/DiffViewer";
import ConfirmModal from "../components/ConfirmModal";

export default function TransformPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [analysis, setAnalysis] = useState<AnalysisResult | null>(null);
  const [filePath, setFilePath] = useState("");
  const [targetFramework, setTargetFramework] = useState("net8.0");
  const [result, setResult] = useState<TransformResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [applying, setApplying] = useState(false);
  const [modernizing, setModernizing] = useState(false);
  const [isAiResult, setIsAiResult] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [applied, setApplied] = useState(false);
  const [appliedFilePath, setAppliedFilePath] = useState("");
  const [apiKey, setApiKey] = useState(() => localStorage.getItem("anthropic_api_key") ?? "");
  const [showKeyInput, setShowKeyInput] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);

  useEffect(() => {
    if (!projectId) return;
    getAnalysis(projectId).then((a) => {
      setAnalysis(a);
      if (a.risks.length > 0) setFilePath(a.risks[0].filePath);
    });
  }, [projectId]);

  const filePaths = Array.from(
    new Set(analysis?.risks.map((r) => r.filePath) ?? [])
  );

  const handlePreview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!projectId) return;
    setLoading(true);
    setError(null);
    setApplied(false);
    setIsAiResult(false);
    try {
      const r = await previewTransform(projectId, filePath, targetFramework);
      setResult(r);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Preview failed");
    } finally {
      setLoading(false);
    }
  };

  const handleModernize = () => {
    if (!projectId || !filePath) return;
    setShowConfirm(true);
  };

  const runModernize = async () => {
    if (!projectId) return;
    setShowConfirm(false);
    setModernizing(true);
    setError(null);
    setApplied(false);
    try {
      const r = await modernizeFile(projectId, filePath, targetFramework, apiKey || undefined);
      setResult(r);
      setIsAiResult(true);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 429) {
        setError("Too many AI Modernize requests — you've hit the hourly limit. Please wait before trying again.");
      } else {
        setError(err instanceof Error ? err.message : "Modernization failed");
      }
    } finally {
      setModernizing(false);
    }
  };

  const handleApply = async () => {
    if (!projectId || !result) return;
    setApplying(true);
    setError(null);
    try {
      if (isAiResult) {
        await applyContent(projectId, filePath, result.transformedSource);
      } else {
        await applyTransform(projectId, filePath, targetFramework);
      }
      setApplied(true);
      setAppliedFilePath(filePath);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Apply failed");
    } finally {
      setApplying(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto p-8 flex flex-col gap-6">
      {showConfirm && (
        <ConfirmModal
          title="AI Modernize"
          message={`This will send ${filePath.split("/").pop()} to Claude for AI modernization.\n\nThis makes a paid API call (~$0.01–0.03).`}
          confirmLabel="Modernize"
          onConfirm={runModernize}
          onCancel={() => setShowConfirm(false)}
        />
      )}
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Transform</h1>
        <Link to={`/dashboard/${projectId}`} className="text-brand-500 hover:underline text-sm">
          ← Dashboard
        </Link>
      </div>

      <form onSubmit={handlePreview} className="rounded-xl bg-gray-900 border border-gray-700 p-6 flex flex-col gap-4">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-1">File</label>
            {filePaths.length > 0 ? (
              <select
                value={filePath}
                onChange={(e) => { setFilePath(e.target.value); setResult(null); setApplied(false); }}
                className="w-full rounded-lg bg-gray-800 border border-gray-700 px-4 py-2 text-gray-100 focus:outline-none focus:ring-2 focus:ring-brand-500"
              >
                {filePaths.map((p) => (
                  <option key={p} value={p}>{p.split("/").pop()}</option>
                ))}
              </select>
            ) : (
              <input
                type="text"
                value={filePath}
                onChange={(e) => setFilePath(e.target.value)}
                placeholder="No flagged files — enter path manually"
                required
                className="w-full rounded-lg bg-gray-800 border border-gray-700 px-4 py-2 text-gray-100 focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            )}
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

        {filePath && (
          <p className="text-xs text-gray-500 truncate" title={filePath}>{filePath}</p>
        )}

        <div className="flex gap-3 flex-wrap items-center">
          <button
            type="submit"
            disabled={loading || !filePath}
            className="rounded-lg bg-brand-600 hover:bg-brand-700 disabled:opacity-50 px-6 py-2 font-semibold transition-colors"
          >
            {loading ? "Previewing…" : "Preview Transform"}
          </button>
          <button
            type="button"
            onClick={handleModernize}
            disabled={modernizing || !filePath}
            className="rounded-lg bg-purple-700 hover:bg-purple-600 disabled:opacity-50 px-6 py-2 font-semibold transition-colors"
          >
            {modernizing ? "Modernizing…" : "✦ AI Modernize"}
          </button>
          <button
            type="button"
            onClick={() => setShowKeyInput((v) => !v)}
            className="ml-auto text-xs text-gray-500 hover:text-gray-300 transition-colors"
          >
            {apiKey ? "🔑 API key set" : "🔑 Set API key"}
          </button>
        </div>

        {showKeyInput && (
          <div className="flex gap-2 items-center">
            <input
              type="password"
              value={apiKey}
              onChange={(e) => {
                setApiKey(e.target.value);
                localStorage.setItem("anthropic_api_key", e.target.value);
              }}
              placeholder="sk-ant-..."
              className="flex-1 rounded-lg bg-gray-800 border border-gray-700 px-4 py-2 text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
            />
            {apiKey && (
              <button
                type="button"
                onClick={() => { setApiKey(""); localStorage.removeItem("anthropic_api_key"); }}
                className="text-xs text-red-400 hover:text-red-300"
              >
                Clear
              </button>
            )}
          </div>
        )}
      </form>

      {error && <p className="text-red-400 text-sm">{error}</p>}

      {result && (
        <div className="flex flex-col gap-4">
          <DiffViewer result={result} />

          {!applied && (result.appliedRules.length > 0 || isAiResult) ? (
            <button
              onClick={handleApply}
              disabled={applying || !result.success}
              className="self-start rounded-lg bg-green-700 hover:bg-green-600 disabled:opacity-50 px-6 py-2 font-semibold transition-colors"
            >
              {applying ? "Applying…" : "Apply Transform"}
            </button>
          ) : !applied ? (
            <p className="text-gray-500 text-sm">No automated rules matched — use AI Modernize for this file.</p>
          ) : (
            <div className="flex items-center gap-4">
              <p className="text-green-400 font-medium">Transform applied successfully.</p>
              <button
                onClick={() => projectId && downloadFile(projectId, appliedFilePath)}
                className="rounded-lg bg-brand-600 hover:bg-brand-700 px-5 py-2 text-sm font-semibold transition-colors"
              >
                Download {appliedFilePath.split("/").pop()}
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
