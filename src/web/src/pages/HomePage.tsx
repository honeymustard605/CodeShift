import { useNavigate } from "react-router-dom";
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import FileUpload from "../components/FileUpload";
import { getProjects } from "../api/client";
import type { AnalysisResult, Project } from "../types";

export default function HomePage() {
  const navigate = useNavigate();
  const [recent, setRecent] = useState<Project[]>([]);

  useEffect(() => {
    getProjects().then((projects) => setRecent(projects.slice(0, 5)));
  }, []);

  const handleAnalysisComplete = (projectId: string, _result: AnalysisResult) => {
    navigate(`/dashboard/${projectId}`);
  };

  return (
    <div className="flex flex-col items-center justify-center min-h-screen px-4 gap-10">
      <div className="text-center">
        <h1 className="text-5xl font-black tracking-tight mb-3">
          Code<span className="text-brand-500">Shift</span>
        </h1>
        <p className="text-gray-400 text-lg max-w-xl">
          Analyze legacy .NET codebases — C#, VB.NET, VB6 — and generate
          actionable migration roadmaps to .NET 8.
        </p>
      </div>

      <div className="w-full max-w-lg rounded-2xl bg-gray-900 border border-gray-700 p-8 shadow-2xl">
        <h2 className="text-lg font-semibold mb-6">Start a New Analysis</h2>
        <FileUpload onAnalysisComplete={handleAnalysisComplete} />
      </div>

      <div className="grid grid-cols-3 gap-6 max-w-2xl w-full text-center">
        {[
          { label: "Languages", value: "C# · VB.NET · VB6" },
          { label: "Output", value: "Dependency Graph + Roadmap" },
          { label: "Target", value: ".NET 8" },
        ].map((item) => (
          <div key={item.label} className="rounded-xl bg-gray-900 border border-gray-800 p-4">
            <p className="text-xs text-gray-500 uppercase tracking-widest mb-1">{item.label}</p>
            <p className="font-semibold text-sm">{item.value}</p>
          </div>
        ))}
      </div>

      {recent.length > 0 && (
        <div className="w-full max-w-lg">
          <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-widest mb-3">Recent Projects</h2>
          <div className="flex flex-col gap-2">
            {recent.map((p) => (
              <Link
                key={p.id}
                to={`/dashboard/${p.id}`}
                className="flex items-center justify-between rounded-lg bg-gray-900 border border-gray-800 px-4 py-3 hover:border-gray-600 transition-colors"
              >
                <span className="font-medium text-sm">{p.name}</span>
                <span className="text-xs text-gray-500">{new Date(p.createdAt).toLocaleDateString()}</span>
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
