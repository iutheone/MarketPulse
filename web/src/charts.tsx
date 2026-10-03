import type { Anomaly, Bar } from "./types";

const PIPELINE = [
  { id: "src", label: "Provider / CSV" },
  { id: "bus", label: "normalized" },
  { id: "feat", label: "Redis features" },
  { id: "eng", label: "Score engine" },
  { id: "pg", label: "Postgres" },
  { id: "hub", label: "Hub + REST" }
] as const;

export function PipelineFlow() {
  return (
    <div className="pipeline" aria-label="Detection pipeline">
      <div className="pipeline-track">
        <span className="pipeline-packet" aria-hidden="true" />
      </div>
      <ol className="pipeline-steps">
        {PIPELINE.map((stage, index) => (
          <li key={stage.id} style={{ animationDelay: `${index * 80}ms` }}>
            <span className="pipeline-idx">{String(index + 1).padStart(2, "0")}</span>
            <strong>{stage.label}</strong>
          </li>
        ))}
      </ol>
    </div>
  );
}

export function Sparkline({
  values,
  className = ""
}: {
  values: number[];
  className?: string;
}) {
  if (values.length < 2) {
    return <div className={`spark empty-spark ${className}`}>Awaiting series</div>;
  }

  const width = 320;
  const height = 72;
  const min = Math.min(...values);
  const max = Math.max(...values);
  const span = max - min || 1;
  const points = values
    .map((value, index) => {
      const x = (index / (values.length - 1)) * width;
      const y = height - ((value - min) / span) * (height - 8) - 4;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(" ");

  return (
    <svg className={`spark ${className}`} viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Score series">
      <polyline fill="none" stroke="currentColor" strokeWidth="2.2" points={points} />
    </svg>
  );
}

export function VolumeChart({ bars }: { bars: Bar[] }) {
  const series = [...bars].slice(0, 48).reverse();
  if (series.length === 0) {
    return null;
  }

  const maxVol = Math.max(...series.map((bar) => bar.volume), 1);
  const closes = series.map((bar) => bar.close);
  const minClose = Math.min(...closes);
  const maxClose = Math.max(...closes);
  const closeSpan = maxClose - minClose || 1;

  return (
    <div className="volume-chart" aria-label="Close and volume">
      <svg viewBox="0 0 480 120" className="volume-svg">
        {series.map((bar, index) => {
          const x = (index / Math.max(series.length - 1, 1)) * 460 + 10;
          const h = (bar.volume / maxVol) * 48;
          const y = 112 - h;
          const cy = 52 - ((bar.close - minClose) / closeSpan) * 40;
          const up = bar.close >= bar.open;
          return (
            <g key={bar.eventId}>
              <rect x={x} y={y} width="5" height={Math.max(2, h)} rx="1" className={up ? "bar-up" : "bar-down"} />
              {index > 0 ? (
                <line
                  x1={(index - 1) / Math.max(series.length - 1, 1) * 460 + 12}
                  y1={52 - ((closes[index - 1] - minClose) / closeSpan) * 40}
                  x2={x + 2}
                  y2={cy}
                  className="close-line"
                />
              ) : null}
            </g>
          );
        })}
      </svg>
      <div className="chart-legend">
        <span>Close path</span>
        <span>Volume columns</span>
      </div>
    </div>
  );
}

export function SeverityMix({ rows }: { rows: Anomaly[] }) {
  const buckets = [
    { key: "Elevated", color: "var(--elevated)" },
    { key: "High", color: "var(--high)" },
    { key: "Extreme", color: "var(--extreme)" }
  ] as const;
  const total = Math.max(rows.length, 1);

  return (
    <div className="severity-mix">
      {buckets.map((bucket) => {
        const count = rows.filter((row) => row.severity === bucket.key).length;
        const pct = (count / total) * 100;
        return (
          <div key={bucket.key} className="mix-row">
            <span>{bucket.key}</span>
            <div className="mix-track">
              <div className="mix-fill" style={{ width: `${pct}%`, background: bucket.color }} />
            </div>
            <em>{count}</em>
          </div>
        );
      })}
    </div>
  );
}

export function FeatureBars({
  rvol,
  price,
  vwap
}: {
  rvol: number | null;
  price: number;
  vwap: number;
}) {
  const items = [
    { label: "RVOL vs 5×", value: Math.min(100, ((rvol ?? 0) / 5) * 100) },
    { label: "|Δ price| vs 5%", value: Math.min(100, (Math.abs(price) / 5) * 100) },
    { label: "|VWAP dev| vs 3%", value: Math.min(100, (Math.abs(vwap) / 3) * 100) }
  ];

  return (
    <div className="feature-bars">
      {items.map((item) => (
        <div key={item.label} className="mix-row">
          <span>{item.label}</span>
          <div className="mix-track">
            <div className="mix-fill accent" style={{ width: `${item.value}%` }} />
          </div>
          <em>{item.value.toFixed(0)}</em>
        </div>
      ))}
    </div>
  );
}
