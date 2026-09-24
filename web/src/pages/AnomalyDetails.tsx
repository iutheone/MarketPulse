import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api";
import type { Anomaly } from "../types";
import { Disclaimer, SeverityBadge, formatNumber, formatTime } from "../ui";

export function AnomalyDetailsPage() {
  const { id = "" } = useParams();
  const [anomaly, setAnomaly] = useState<Anomaly | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    api
      .getAnomaly(id)
      .then((row) => {
        if (!cancelled) {
          setAnomaly(row);
          setError(null);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "Not found");
      });
    return () => {
      cancelled = true;
    };
  }, [id]);

  return (
    <section>
      <h2>Anomaly</h2>
      <Disclaimer />
      {error ? <p className="error">{error}</p> : null}
      {anomaly ? (
        <>
          <div className="grid">
            <div className="card">
              <div className="label">Symbol</div>
              <div className="value">
                <Link to={`/stocks/${anomaly.symbol}`}>{anomaly.symbol}</Link>
              </div>
            </div>
            <div className="card">
              <div className="label">Score</div>
              <div className="value">{formatNumber(anomaly.score, 1)}</div>
            </div>
            <div className="card">
              <div className="label">Severity</div>
              <div className="value">
                <SeverityBadge severity={anomaly.severity} />
              </div>
            </div>
            <div className="card">
              <div className="label">Detected</div>
              <div className="value" style={{ fontSize: 16 }}>
                {formatTime(anomaly.detectedAt)}
              </div>
            </div>
          </div>
          <p>
            Price {formatNumber(anomaly.lastPrice)} · volume 1m {formatNumber(anomaly.volume1m, 0)} · RVOL{" "}
            {formatNumber(anomaly.relativeVolume)} · price {formatNumber(anomaly.priceChangePercent)}% · VWAP{" "}
            {formatNumber(anomaly.vwapDeviationPercent)}%
          </p>
          <h3>Reasons</h3>
          <ul>
            {anomaly.reasons.map((reason) => (
              <li key={reason}>{reason}</li>
            ))}
          </ul>
          <p className="muted">
            Rule {anomaly.ruleVersion} · event {anomaly.sourceEventId}
          </p>
        </>
      ) : null}
    </section>
  );
}
