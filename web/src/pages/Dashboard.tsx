import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { connectAnomalyHub, type HubStatus } from "../signalr";
import type { Anomaly, AnomalyLive } from "../types";
import { Disclaimer, SeverityBadge, formatNumber, formatTime } from "../ui";

export function DashboardPage() {
  const [symbol, setSymbol] = useState("");
  const [severity, setSeverity] = useState("");
  const [rows, setRows] = useState<Anomaly[]>([]);
  const [total, setTotal] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<HubStatus>("disconnected");
  const filtersRef = useRef({ symbol, severity });
  filtersRef.current = { symbol, severity };

  const load = useCallback(async () => {
    const params = new URLSearchParams({ page: "1", pageSize: "40", sort: "detectedAt", direction: "desc" });
    if (symbol.trim()) params.set("symbol", symbol.trim().toUpperCase());
    if (severity) params.set("severity", severity);
    try {
      const page = await api.listAnomalies(params);
      setRows(page.items);
      setTotal(page.total);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load anomalies");
    }
  }, [symbol, severity]);

  const loadRef = useRef(load);
  loadRef.current = load;

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    const connection = connectAnomalyHub({
      onStatus: setStatus,
      onResync: () => {
        void loadRef.current();
      },
      onAnomaly: (live: AnomalyLive) => {
        const filters = filtersRef.current;
        if (filters.symbol.trim() && live.symbol !== filters.symbol.trim().toUpperCase()) {
          return;
        }
        if (filters.severity && live.severity !== filters.severity) {
          return;
        }
        void (async () => {
          try {
            const full = await api.getAnomaly(live.id);
            setRows((current) => [full, ...current.filter((row) => row.id !== full.id)].slice(0, 40));
          } catch {
            setRows((current) => {
              if (current.some((row) => row.id === live.id)) return current;
              const stub: Anomaly = {
                id: live.id,
                sourceEventId: live.id,
                symbol: live.symbol,
                detectedAt: live.timestamp,
                score: live.score,
                severity: live.severity,
                reasons: live.reasons,
                ruleVersion: "",
                relativeVolume: null,
                volumeAcceleration: null,
                priceChangePercent: 0,
                vwapDeviationPercent: 0,
                breakout: false,
                lastPrice: 0,
                volume1m: 0
              };
              return [stub, ...current].slice(0, 40);
            });
          }
        })();
      }
    });

    return () => {
      void connection.stop();
    };
  }, []);

  const empty = useMemo(
    () => rows.length === 0 && !error,
    [rows, error]
  );

  return (
    <section>
      <h2>Dashboard</h2>
      <p className="lede">
        Latest recorded anomalies. Live rows arrive over SignalR; a reconnect reloads this list from REST.
      </p>
      <Disclaimer />
      <div className="toolbar">
        <input
          placeholder="Symbol"
          value={symbol}
          onChange={(event) => setSymbol(event.target.value)}
          aria-label="Filter symbol"
        />
        <select value={severity} onChange={(event) => setSeverity(event.target.value)} aria-label="Filter severity">
          <option value="">All severities</option>
          <option>Elevated</option>
          <option>High</option>
          <option>Extreme</option>
        </select>
        <button className="primary" onClick={() => void load()}>
          Search
        </button>
        <span className="live">
          Hub <span className={`badge ${status}`}>{status}</span> · {total} stored
        </span>
      </div>
      {error ? <p className="error">{error}</p> : null}
      {empty ? <p className="muted">No anomalies yet. Quiet prints stay below the record threshold.</p> : null}
      {rows.length > 0 ? (
        <table>
          <thead>
            <tr>
              <th>Symbol</th>
              <th>Price</th>
              <th>Volume 1m</th>
              <th>RVOL</th>
              <th>Price chg</th>
              <th>Score</th>
              <th>Severity</th>
              <th>Time</th>
              <th>Reason</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.id}>
                <td>
                  <Link to={`/stocks/${row.symbol}`}>{row.symbol}</Link>
                </td>
                <td>{formatNumber(row.lastPrice)}</td>
                <td>{formatNumber(row.volume1m, 0)}</td>
                <td>{formatNumber(row.relativeVolume)}</td>
                <td>{formatNumber(row.priceChangePercent)}%</td>
                <td>
                  <Link to={`/anomalies/${row.id}`}>{formatNumber(row.score, 1)}</Link>
                </td>
                <td>
                  <SeverityBadge severity={row.severity} />
                </td>
                <td>{formatTime(row.detectedAt)}</td>
                <td>{row.reasons[0] ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}
    </section>
  );
}
