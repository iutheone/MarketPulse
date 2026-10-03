import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import { connectAnomalyHub, type HubStatus } from "../signalr";
import type { Anomaly, AnomalyLive } from "../types";
import { PipelineFlow, SeverityMix, Sparkline } from "../charts";
import { Disclaimer, EmptyState, PageHeader, ScoreMeter, SeverityBadge, formatNumber, formatTime } from "../ui";

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

  const stats = useMemo(() => {
    const peak = rows.reduce((max, row) => Math.max(max, row.score), 0);
    const symbols = new Set(rows.map((row) => row.symbol)).size;
    return { peak, symbols };
  }, [rows]);

  const scoreSeries = useMemo(
    () => [...rows].reverse().map((row) => row.score),
    [rows]
  );

  return (
    <section>
      <PageHeader kicker="Ops console · live" title="Anomaly deck">
        <p className="lede">
          Recorded unusual volume and price prints. Live rows arrive over SignalR; reconnect reloads from REST. This is
          not a forecast.
        </p>
      </PageHeader>
      <Disclaimer />
      <PipelineFlow />
      <div className="grid stats-grid">
        <div className="card stat">
          <div className="label">Stored</div>
          <div className="value">{total}</div>
        </div>
        <div className="card stat">
          <div className="label">In view</div>
          <div className="value">{rows.length}</div>
        </div>
        <div className="card stat">
          <div className="label">Peak score</div>
          <div className="value">{formatNumber(stats.peak, 1)}</div>
        </div>
        <div className="card stat">
          <div className="label">Symbols</div>
          <div className="value">{stats.symbols}</div>
        </div>
      </div>
      {rows.length > 0 ? (
        <div className="chart-grid">
          <article className="card chart-card">
            <div className="label">Score tape (oldest → newest in view)</div>
            <Sparkline values={scoreSeries} />
          </article>
          <article className="card chart-card">
            <div className="label">Severity mix</div>
            <SeverityMix rows={rows} />
          </article>
        </div>
      ) : null}
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
          Hub <span className={`badge ${status} ${status === "connected" ? "pulse" : ""}`}>{status}</span> · {total}{" "}
          stored
        </span>
      </div>
      {error ? <p className="error">{error}</p> : null}
      {empty ? (
        <EmptyState>No anomalies yet. Quiet prints stay below the record threshold.</EmptyState>
      ) : null}
      {rows.length > 0 ? (
        <div className="table-wrap">
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
            {rows.map((row, index) => (
              <tr key={row.id} style={{ animationDelay: `${Math.min(index, 12) * 40}ms` }}>
                <td>
                  <Link className="ticker" to={`/stocks/${row.symbol}`}>
                    {row.symbol}
                  </Link>
                </td>
                <td>{formatNumber(row.lastPrice)}</td>
                <td>{formatNumber(row.volume1m, 0)}</td>
                <td>{formatNumber(row.relativeVolume)}</td>
                <td>{formatNumber(row.priceChangePercent)}%</td>
                <td>
                  <Link to={`/anomalies/${row.id}`}>
                    <ScoreMeter score={row.score} severity={row.severity} />
                  </Link>
                </td>
                <td>
                  <SeverityBadge severity={row.severity} />
                </td>
                <td>{formatTime(row.detectedAt)}</td>
                <td className="reason">{row.reasons[0] ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      ) : null}
    </section>
  );
}
