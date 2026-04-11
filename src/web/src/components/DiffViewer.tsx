import ReactDiffViewer from "react-diff-viewer-continued";
import type { TransformResult } from "../types";

interface DiffViewerProps {
  result: TransformResult;
}

export default function DiffViewer({ result }: DiffViewerProps) {
  return (
    <div className="flex flex-col gap-4">
      {result.appliedRules.length > 0 && (
        <div className="rounded-lg bg-green-900/30 border border-green-700 px-4 py-3">
          <p className="text-sm font-semibold text-green-300 mb-1">Applied Rules</p>
          <ul className="list-disc list-inside text-xs text-green-400 space-y-0.5">
            {result.appliedRules.map((r, i) => <li key={i}>{r}</li>)}
          </ul>
        </div>
      )}

      {result.warnings.length > 0 && (
        <div className="rounded-lg bg-yellow-900/30 border border-yellow-700 px-4 py-3">
          <p className="text-sm font-semibold text-yellow-300 mb-1">Warnings</p>
          <ul className="list-disc list-inside text-xs text-yellow-400 space-y-0.5">
            {result.warnings.map((w, i) => <li key={i}>{w}</li>)}
          </ul>
        </div>
      )}

      <div className="rounded-xl overflow-hidden border border-gray-700 text-sm">
        <ReactDiffViewer
          oldValue={result.originalSource}
          newValue={result.transformedSource}
          splitView={false}
          useDarkTheme
          leftTitle="Original"
          rightTitle="Transformed"
        />
      </div>
    </div>
  );
}
