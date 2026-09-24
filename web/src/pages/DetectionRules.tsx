import { FormEvent, useEffect, useState } from "react";
import { api } from "../api";
import type { DetectionRule } from "../types";
import { Disclaimer, formatTime } from "../ui";

export function DetectionRulesPage() {
  const [rules, setRules] = useState<DetectionRule[]>([]);
  const [version, setVersion] = useState("v2");
  const [parametersJson, setParametersJson] = useState('{"minScoreToRecord":40}');
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setRules(await api.rules());
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load rules");
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    try {
      JSON.parse(parametersJson);
      await api.createRule({ version, isActive: false, parametersJson });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Invalid JSON");
    }
  }

  async function toggle(rule: DetectionRule) {
    try {
      await api.updateRule(rule.id, {
        version: rule.version,
        isActive: !rule.isActive,
        parametersJson: rule.parametersJson
      });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Update failed");
    }
  }

  return (
    <section>
      <h2>Detection rules</h2>
      <p className="lede">
        Catalog only. The worker still scores from AnomalyDetection options until it is restarted.
      </p>
      <Disclaimer />
      {error ? <p className="error">{error}</p> : null}
      <form className="stack" onSubmit={(event) => void onSubmit(event)}>
        <input value={version} onChange={(event) => setVersion(event.target.value)} placeholder="Version" required />
        <textarea
          value={parametersJson}
          onChange={(event) => setParametersJson(event.target.value)}
          rows={5}
          aria-label="Parameters JSON"
        />
        <button className="primary" type="submit">
          Add inactive rule
        </button>
      </form>
      {rules.length === 0 ? <p className="muted">No rules seeded yet. Start the API once so migrations run.</p> : null}
      <table>
        <thead>
          <tr>
            <th>Version</th>
            <th>Active</th>
            <th>Created</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {rules.map((rule) => (
            <tr key={rule.id}>
              <td>{rule.version}</td>
              <td>{rule.isActive ? "yes" : "no"}</td>
              <td>{formatTime(rule.createdAt)}</td>
              <td>
                <button onClick={() => void toggle(rule)}>{rule.isActive ? "Disable" : "Enable"}</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
