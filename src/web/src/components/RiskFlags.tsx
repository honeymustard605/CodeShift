import type { RiskFlag, RiskLevel } from "../types";
import clsx from "clsx";

const levelStyles: Record<RiskLevel, string> = {
  Low: "bg-green-900/40 text-green-300 border-green-700",
  Medium: "bg-yellow-900/40 text-yellow-300 border-yellow-700",
  High: "bg-orange-900/40 text-orange-300 border-orange-700",
  Critical: "bg-red-900/40 text-red-300 border-red-700",
};

interface RiskFlagsProps {
  risks: RiskFlag[];
}

export default function RiskFlags({ risks }: RiskFlagsProps) {
  if (risks.length === 0) {
    return <p className="text-gray-400 text-sm">No risk flags detected.</p>;
  }

  const sorted = [...risks].sort((a, b) => {
    const order: Record<RiskLevel, number> = { Critical: 0, High: 1, Medium: 2, Low: 3 };
    return order[a.level] - order[b.level];
  });

  return (
    <ul className="flex flex-col gap-3">
      {sorted.map((risk, i) => (
        <li
          key={i}
          className={clsx(
            "rounded-lg border px-4 py-3",
            levelStyles[risk.level]
          )}
        >
          <div className="flex items-center gap-2 mb-1">
            <span className="text-xs font-bold uppercase tracking-wide">
              {risk.level}
            </span>
            <span className="font-semibold text-sm">{risk.category}</span>
          </div>
          <p className="text-xs opacity-80">{risk.description}</p>
          <p className="text-xs opacity-50 mt-1 truncate">{risk.filePath}</p>
        </li>
      ))}
    </ul>
  );
}
