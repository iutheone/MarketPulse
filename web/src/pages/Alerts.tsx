import { FormEvent, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import type { AlertConfiguration, AlertDelivery } from "../types";
import { Disclaimer, formatTime } from "../ui";

export function AlertsPage() {
  const [configs, setConfigs] = useState<AlertConfiguration[]>([]);
  const [deliveries, setDeliveries] = useState<AlertDelivery[]>([]);
  const [symbol, setSymbol] = useState("*");
  const [minSeverity, setMinSeverity] = useState("High");
  const [destination, setDestination] = useState("http://127.0.0.1:9/hook");
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setConfigs(await api.alertConfigurations());
      setDeliveries(await api.alertDeliveries());
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load alerts");
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    try {
      await api.createAlertConfiguration({
        symbol,
        minSeverity,
        channel: "webhook",
        destination,
        isEnabled: true
      });
      setError(null);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Save failed");
    }
  }

  async function onDelete(id: string) {
    try {
      await api.deleteAlertConfiguration(id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Delete failed");
    }
  }

  return (
    <section>
      <h2>Alerts</h2>
      <p className="lede">
        Webhooks fire after an anomaly is recorded. Delivery is asynchronous (Kafka outbox). This is not a trading
        signal.
      </p>
      <Disclaimer />
      {error ? <p className="error">{error}</p> : null}
      <form className="stack" onSubmit={(event) => void onSubmit(event)}>
        <input value={symbol} onChange={(event) => setSymbol(event.target.value)} placeholder="Symbol or *" />
        <select value={minSeverity} onChange={(event) => setMinSeverity(event.target.value)} aria-label="Min severity">
          <option>Elevated</option>
          <option>High</option>
          <option>Extreme</option>
        </select>
        <input
          value={destination}
          onChange={(event) => setDestination(event.target.value)}
          placeholder="http://127.0.0.1:9/hook"
          required
        />
        <button className="primary" type="submit">
          Save webhook
        </button>
      </form>
      {configs.length === 0 ? <p className="muted">No webhook configs yet.</p> : null}
      {configs.map((config) => (
        <article className="card" key={config.id} style={{ marginBottom: 10 }}>
          <strong>
            {config.symbol} ≥ {config.minSeverity}
          </strong>
          <p className="muted">{config.destination}</p>
          <button type="button" onClick={() => void onDelete(config.id)}>
            Remove
          </button>
        </article>
      ))}
      <h3>Recent deliveries</h3>
      {deliveries.length === 0 ? <p className="muted">No deliveries yet.</p> : null}
      {deliveries.map((row) => (
        <article className="card" key={row.id} style={{ marginBottom: 10 }}>
          <strong>{row.status}</strong> · attempt {row.attemptCount} · {formatTime(row.attemptedAt)}
          <p>
            <Link to={`/anomalies/${row.anomalyId}`}>{row.anomalyId}</Link>
          </p>
          {row.error ? <p className="muted">{row.error}</p> : null}
        </article>
      ))}
    </section>
  );
}
