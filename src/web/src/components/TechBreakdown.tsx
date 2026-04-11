import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Cell } from "recharts";
import type { DetectedProject } from "../types";

interface TechBreakdownProps {
  projects: DetectedProject[];
}

const frameworkColor: Record<string, string> = {
  "net8.0": "#22c55e",
  "net6.0": "#84cc16",
  netcoreapp: "#3b82f6",
  net4x: "#f59e0b",
  vb6: "#ef4444",
  unknown: "#6b7280",
};

export default function TechBreakdown({ projects }: TechBreakdownProps) {
  const data = projects.map((p) => ({
    name: p.name.length > 20 ? p.name.slice(0, 18) + "…" : p.name,
    files: p.fileCount,
    framework: p.targetFramework,
  }));

  return (
    <div className="w-full h-64">
      <ResponsiveContainer>
        <BarChart data={data} layout="vertical" margin={{ left: 16 }}>
          <XAxis type="number" stroke="#6b7280" />
          <YAxis type="category" dataKey="name" stroke="#6b7280" width={120} tick={{ fontSize: 11 }} />
          <Tooltip
            contentStyle={{ background: "#1e293b", border: "1px solid #334155", borderRadius: 8 }}
          />
          <Bar dataKey="files" radius={[0, 4, 4, 0]}>
            {data.map((entry, i) => (
              <Cell
                key={i}
                fill={frameworkColor[entry.framework] ?? frameworkColor.unknown}
              />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
