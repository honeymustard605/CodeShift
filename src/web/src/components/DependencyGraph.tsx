import { useCallback, useRef } from "react";
import ForceGraph2D, { type ForceGraphMethods } from "react-force-graph-2d";
import type { DependencyEdge } from "../types";

interface DependencyGraphProps {
  nodes: string[];
  edges: DependencyEdge[];
}

export default function DependencyGraph({ nodes, edges }: DependencyGraphProps) {
  const fgRef = useRef<ForceGraphMethods | undefined>(undefined);

  const graphData = {
    nodes: nodes.map((id) => ({ id })),
    links: edges.map((e) => ({ source: e.source, target: e.target, kind: e.kind })),
  };

  const handleEngineStop = useCallback(() => {
    fgRef.current?.zoomToFit(400, 40);
  }, []);

  return (
    <div className="w-full h-[600px] rounded-xl overflow-hidden border border-gray-700 bg-gray-950">
      <ForceGraph2D
        ref={fgRef}
        graphData={graphData}
        onEngineStop={handleEngineStop}
        nodeAutoColorBy="id"
        nodeLabel="id"
        linkLabel="kind"
        nodeCanvasObject={(node, ctx, globalScale) => {
          const label = String(node.id);
          const fontSize = 12 / globalScale;
          ctx.font = `${fontSize}px Sans-Serif`;
          const textWidth = ctx.measureText(label).width;
          const bckgDimensions: [number, number] = [textWidth + 8, fontSize + 4];

          ctx.fillStyle = "rgba(30, 41, 59, 0.9)";
          ctx.fillRect(
            (node.x ?? 0) - bckgDimensions[0] / 2,
            (node.y ?? 0) - bckgDimensions[1] / 2,
            ...bckgDimensions
          );

          ctx.textAlign = "center";
          ctx.textBaseline = "middle";
          ctx.fillStyle = "#e2e8f0";
          ctx.fillText(label, node.x ?? 0, node.y ?? 0);
        }}
        linkColor={() => "#475569"}
        backgroundColor="#030712"
        width={undefined}
        height={600}
      />
    </div>
  );
}
