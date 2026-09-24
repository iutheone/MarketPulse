import { FormEvent, useEffect, useState } from "react";
import { api } from "../api";
import type { BacktestRun, ReplayResult } from "../types";
import { Disclaimer, formatNumber, formatTime } from "../ui";

export function BacktestsPage() {
  const [runs, setRuns] = useState<BacktestRun[]>([]);
  const [source, setSource] = useState("csv");
  const [csvFile, setCsvFile] = useState("replay-aapl.csv");
  const [symbols, setSymbols] = useState("AAPL");
  const [error, setError] = useState<string | null>(null);
  const [replayNote, setReplayNote] = useState<ReplayResult | null>(null);

  async function load() {
    try {
      setRuns(await api.backtests());
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load backtests");
    }
  }

  useEffect(() => {
    void load();
  }, []);

  function payload() {
    return {
      source,
      csvFile: source === "csv" ? csvFile : undefined,
      symbols: symbols
        .split(/[\s,]+/)
        .map((item) => item.trim().toUpperCase())
        .filter(Boolean)
    };
  }

  async function onBacktest(event: FormEvent) {
    event.preventDefault();
    try {
      await api.runBacktest(payload());
      await load();
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Backtest failed");
    }
  }

  async function onReplay() {
    try {
      setReplayNote(await api.replay(payload()));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Replay failed");
    }
  }

  return (
    <section>
      <h2>Replay &amp; backtest</h2>
      <p className="lede">
        CSV replay publishes to <code>market.normalized</code> like live ticks. Backtest walks the same engine in
        process and never produces a trading result.
      </p>
      <Disclaimer />
      {error ? <p className="error">{error}</p> : null}
      <form className="stack" onSubmit={(event) => void onBacktest(event)}>
        <select value={source} onChange={(event) => setSource(event.target.value)} aria-label="Source">
          <option value="csv">csv</option>
          <option value="postgres">postgres</option>
        </select>
        <input value={csvFile} onChange={(event) => setCsvFile(event.target.value)} placeholder="CSV file name" />
        <input value={symbols} onChange={(event) => setSymbols(event.target.value)} placeholder="Symbols" />
        <button className="primary" type="submit">
          Run backtest
        </button>
        <button type="button" onClick={() => void onReplay()}>
          Publish replay to Kafka
        </button>
      </form>
      {replayNote ? (
        <p className="muted">
          Replay published {replayNote.published} / {replayNote.barsRead} ticks. {replayNote.note}
        </p>
      ) : null}
      {runs.length === 0 ? <p className="muted">No backtest runs stored yet.</p> : null}
      {runs.map((run) => (
        <article className="card" key={run.id} style={{ marginBottom: 10 }}>
          <strong>{run.status}</strong> · {formatTime(run.startedAt)}
          {run.results.map((result) => (
            <p key={result.id}>
              {result.symbol}: {result.anomalies}/{result.bars} recorded (rate {formatNumber(result.recordRate, 4)})
              {result.meanScore != null ? ` · mean score ${formatNumber(result.meanScore, 1)}` : ""}
            </p>
          ))}
          <p className="muted">{run.results[0]?.notes ?? run.error}</p>
        </article>
      ))}
    </section>
  );
}
