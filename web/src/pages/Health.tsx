import { useEffect, useState } from "react";
import { api } from "../api";
import type { SystemHealth } from "../types";
import { PipelineFlow } from "../charts";
import { Disclaimer, PageHeader } from "../ui";

export function HealthPage() {
  const [health, setHealth] = useState<SystemHealth | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setHealth(await api.health());
      setError(null);
    } catch (err) {
      setHealth(null);
      setError(err instanceof Error ? err.message : "Health endpoint unreachable");
    }
  }

  useEffect(() => {
    void load();
    const timer = window.setInterval(() => void load(), 8000);
    return () => window.clearInterval(timer);
  }, []);

  return (
    <section>
      <PageHeader kicker="Operations" title="System health">
        <p className="lede">
          Kafka, Redis, and PostgreSQL as reported by the API process. Metrics:{" "}
          <a href={`${api.url}/metrics`} target="_blank" rel="noreferrer">
            {api.url}/metrics
          </a>
          . Grafana (local compose):{" "}
          <a href="http://localhost:3000" target="_blank" rel="noreferrer">
            http://localhost:3000
          </a>{" "}
          (admin/admin).
        </p>
      </PageHeader>
      <Disclaimer />
      <PipelineFlow />
      {error ? <p className="error">{error}</p> : null}
      {health ? (
        <>
          <div className="grid stats-grid">
            <div className="card stat">
              <div className="label">Cluster</div>
              <div className="value">
                <span className={`badge ${health.status}`}>{health.status}</span>
              </div>
            </div>
            <div className="card stat">
              <div className="label">Probe</div>
              <div className="value">{health.totalDurationMs.toFixed(0)} ms</div>
            </div>
          </div>
          <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Check</th>
                <th>Status</th>
                <th>Detail</th>
                <th>ms</th>
              </tr>
            </thead>
            <tbody>
              {health.checks.map((check) => (
                <tr key={check.name}>
                  <td>{check.name}</td>
                  <td>
                    <span className={`badge ${check.status}`}>{check.status}</span>
                  </td>
                  <td className="muted">{check.description}</td>
                  <td>{check.durationMs.toFixed(0)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        </>
      ) : null}
    </section>
  );
}
