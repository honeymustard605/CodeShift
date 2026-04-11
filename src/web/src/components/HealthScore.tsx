interface HealthScoreProps {
  score: number;
}

export default function HealthScore({ score }: HealthScoreProps) {
  const color =
    score >= 75
      ? "text-green-400"
      : score >= 50
        ? "text-yellow-400"
        : score >= 25
          ? "text-orange-400"
          : "text-red-500";

  const label =
    score >= 75 ? "Healthy" : score >= 50 ? "Moderate Risk" : score >= 25 ? "High Risk" : "Critical";

  const circumference = 2 * Math.PI * 40;
  const offset = circumference - (score / 100) * circumference;

  return (
    <div className="flex flex-col items-center gap-2">
      <div className="relative w-28 h-28">
        <svg className="rotate-[-90deg]" width="112" height="112">
          <circle cx="56" cy="56" r="40" fill="none" stroke="#1f2937" strokeWidth="10" />
          <circle
            cx="56"
            cy="56"
            r="40"
            fill="none"
            stroke="currentColor"
            strokeWidth="10"
            strokeDasharray={circumference}
            strokeDashoffset={offset}
            strokeLinecap="round"
            className={color}
          />
        </svg>
        <span className={`absolute inset-0 flex items-center justify-center text-2xl font-bold ${color}`}>
          {score}
        </span>
      </div>
      <span className={`text-sm font-medium ${color}`}>{label}</span>
    </div>
  );
}
