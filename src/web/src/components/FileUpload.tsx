import { useState, useCallback } from "react";
import { useDropzone } from "react-dropzone";
import { createProject, uploadAndAnalyze } from "../api/client";
import type { AnalysisResult } from "../types";

interface FileUploadProps {
  onAnalysisComplete: (projectId: string, result: AnalysisResult) => void;
}

export default function FileUpload({ onAnalysisComplete }: FileUploadProps) {
  const [projectName, setProjectName] = useState("");
  const [zipFile, setZipFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onDrop = useCallback((acceptedFiles: File[]) => {
    const file = acceptedFiles[0];
    if (!file) return;
    setZipFile(file);
    if (!projectName) {
      setProjectName(file.name.replace(/\.zip$/i, ""));
    }
  }, [projectName]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { "application/zip": [".zip"] },
    multiple: false,
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!zipFile) {
      setError("Please select a zip file.");
      return;
    }
    setLoading(true);
    setError(null);

    try {
      const project = await createProject(projectName);
      const result = await uploadAndAnalyze(project.id, zipFile);
      onAnalysisComplete(project.id, result);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Analysis failed");
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 w-full max-w-lg">
      <div
        {...getRootProps()}
        className={`rounded-xl border-2 border-dashed px-6 py-8 text-center cursor-pointer transition-colors ${
          isDragActive
            ? "border-brand-500 bg-brand-500/10"
            : zipFile
            ? "border-green-600 bg-green-600/10"
            : "border-gray-600 hover:border-gray-500"
        }`}
      >
        <input {...getInputProps()} />
        {zipFile ? (
          <>
            <p className="text-green-400 text-sm font-medium">{zipFile.name}</p>
            <p className="text-gray-500 text-xs mt-1">Click or drop to replace</p>
          </>
        ) : (
          <>
            <p className="text-gray-400 text-sm">
              {isDragActive ? "Drop your zip here…" : "Drag & drop a .zip of your codebase, or click to browse"}
            </p>
            <p className="text-gray-600 text-xs mt-1">
              Supports .cs, .vb, .bas, .cls, .frm files inside the zip
            </p>
          </>
        )}
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-300 mb-1">
          Project Name
        </label>
        <input
          type="text"
          value={projectName}
          onChange={(e) => setProjectName(e.target.value)}
          placeholder="My Legacy App"
          required
          className="w-full rounded-lg bg-gray-800 border border-gray-700 px-4 py-2 text-gray-100 focus:outline-none focus:ring-2 focus:ring-brand-500"
        />
      </div>

      {error && <p className="text-red-400 text-sm">{error}</p>}

      <button
        type="submit"
        disabled={loading || !zipFile}
        className="rounded-lg bg-brand-600 hover:bg-brand-700 disabled:opacity-50 px-6 py-2 font-semibold transition-colors"
      >
        {loading ? "Analyzing…" : "Analyze Codebase"}
      </button>
    </form>
  );
}
