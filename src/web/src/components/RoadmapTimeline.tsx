import type { MigrationRoadmap, RiskLevel } from "../types";
import clsx from "clsx";

const complexityBadge: Record<RiskLevel, string> = {
  Low: "bg-green-800 text-green-200",
  Medium: "bg-yellow-800 text-yellow-200",
  High: "bg-orange-800 text-orange-200",
  Critical: "bg-red-800 text-red-200",
};

interface RoadmapTimelineProps {
  roadmap: MigrationRoadmap;
}

export default function RoadmapTimeline({ roadmap }: RoadmapTimelineProps) {
  return (
    <div className="flex flex-col gap-8">
      <div className="flex items-center gap-4">
        <span className="text-2xl font-bold">~{roadmap.estimatedWeeks} weeks</span>
        <span className="text-gray-400">to {roadmap.targetFramework}</span>
      </div>

      <ol className="relative border-l border-gray-700 ml-4">
        {roadmap.phases.map((phase) => (
          <li key={phase.order} className="mb-10 ml-6">
            <span className="absolute -left-3 flex h-6 w-6 items-center justify-center rounded-full bg-brand-600 text-xs font-bold ring-4 ring-gray-950">
              {phase.order}
            </span>

            <div className="ml-2">
              <h3 className="text-lg font-semibold">{phase.name}</h3>
              <p className="text-sm text-gray-400 mb-1">{phase.description}</p>
              <span className="text-xs text-gray-500">{phase.estimatedWeeks}w</span>

              <ul className="mt-3 flex flex-col gap-2">
                {phase.tasks.map((task, ti) => (
                  <li key={ti} className="rounded-lg bg-gray-800/60 px-4 py-3 border border-gray-700">
                    <div className="flex items-center gap-2 mb-1">
                      <span
                        className={clsx(
                          "text-xs px-2 py-0.5 rounded font-medium",
                          complexityBadge[task.complexity]
                        )}
                      >
                        {task.complexity}
                      </span>
                      <span className="font-medium text-sm">{task.title}</span>
                    </div>
                    <p className="text-xs text-gray-400">{task.description}</p>
                  </li>
                ))}
              </ul>
            </div>
          </li>
        ))}
      </ol>
    </div>
  );
}
