import { useEffect, useState } from "react";
import { api } from "../api";
import type { SystemHealth } from "../types";
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
      {error ? <p className="error">{error}</p> : null}
      {health ? (
        <>
          <p>
            Overall <span className={`badge ${health.status}`}>{health.status}</span> in{" "}
            {health.totalDurationMs.toFixed(0)} ms
          </p>
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
