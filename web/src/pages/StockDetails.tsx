import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api";
import type { Bar, Snapshot } from "../types";
import { Disclaimer, SeverityBadge, formatNumber, formatTime } from "../ui";

export function StockDetailsPage() {
  const { symbol = "" } = useParams();
  const [snapshot, setSnapshot] = useState<Snapshot | null>(null);
  const [history, setHistory] = useState<Bar[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const [snapResult, barsResult] = await Promise.allSettled([api.snapshot(symbol), api.history(symbol)]);
        if (cancelled) return;
        const snap = snapResult.status === "fulfilled" ? snapResult.value : null;
        const bars = barsResult.status === "fulfilled" ? barsResult.value.items : [];
        setSnapshot(snap);
        setHistory(bars);
        if (!snap && bars.length === 0) {
          setError("No snapshot or history for this symbol yet. Start the worker to ingest ticks.");
        } else {
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setSnapshot(null);
          setHistory([]);
          setError(err instanceof Error ? err.message : "Symbol not found");
        }
      }
    }
    void load();
    return () => {
      cancelled = true;
    };
  }, [symbol]);

  const features = snapshot?.features;

  return (
    <section>
      <h2>{symbol.toUpperCase()}</h2>
      <p className="lede">Hot snapshot from Redis plus durable bars from PostgreSQL.</p>
      <Disclaimer />
      {error ? <p className="error">{error}</p> : null}
      {snapshot ? (
        <>
          <p className="muted">{snapshot.note}</p>
          <div className="grid">
            <div className="card">
              <div className="label">Last price</div>
              <div className="value">{formatNumber(features?.lastPrice ?? snapshot.latestBar?.close)}</div>
            </div>
            <div className="card">
              <div className="label">Volume 1m</div>
              <div className="value">{formatNumber(features?.volume1m, 0)}</div>
            </div>
            <div className="card">
              <div className="label">RVOL</div>
              <div className="value">{formatNumber(features?.relativeVolume)}</div>
            </div>
            <div className="card">
              <div className="label">Price change</div>
              <div className="value">{formatNumber(features?.priceChangePercent)}%</div>
            </div>
          </div>
          {snapshot.latestAnomaly ? (
            <p>
              Latest anomaly{" "}
              <Link to={`/anomalies/${snapshot.latestAnomaly.id}`}>
                {formatNumber(snapshot.latestAnomaly.score, 1)}
              </Link>{" "}
              <SeverityBadge severity={snapshot.latestAnomaly.severity} />
            </p>
          ) : (
            <p className="muted">No recorded anomaly for this symbol yet.</p>
          )}
        </>
      ) : null}
      <h3>History</h3>
      {history.length === 0 ? <p className="muted">No bars stored.</p> : null}
      {history.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>Time</th>
              <th>Open</th>
              <th>High</th>
              <th>Low</th>
              <th>Close</th>
              <th>Volume</th>
              <th>Source</th>
            </tr>
          </thead>
          <tbody>
            {history.map((bar) => (
              <tr key={bar.eventId}>
                <td>{formatTime(bar.timestamp)}</td>
                <td>{formatNumber(bar.open)}</td>
                <td>{formatNumber(bar.high)}</td>
                <td>{formatNumber(bar.low)}</td>
                <td>{formatNumber(bar.close)}</td>
                <td>{formatNumber(bar.volume, 0)}</td>
                <td>{bar.source}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </section>
  );
}
