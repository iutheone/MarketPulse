import type {
  Anomaly,
  DetectionRule,
  Paged,
  Snapshot,
  SystemHealth,
  Watchlist
} from "./types";

const API_URL = import.meta.env.VITE_API_URL ?? "http://localhost:5082";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers
    }
  });

  if (!response.ok) {
    const body = await response.text();
    if (response.status === 404) {
      throw new Error("Not found");
    }
    throw new Error(body || `${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  url: API_URL,
  listAnomalies: (params: URLSearchParams) =>
    request<Paged<Anomaly>>(`/api/v1/anomalies?${params.toString()}`),
  getAnomaly: (id: string) => request<Anomaly>(`/api/v1/anomalies/${id}`),
  snapshot: (symbol: string) => request<Snapshot>(`/api/v1/stocks/${encodeURIComponent(symbol)}/snapshot`),
  history: (symbol: string, page = 1) =>
    request<Paged<import("./types").Bar>>(
      `/api/v1/stocks/${encodeURIComponent(symbol)}/history?page=${page}&pageSize=40`
    ),
  watchlists: () => request<Watchlist[]>("/api/v1/watchlists"),
  createWatchlist: (name: string, symbols: string[]) =>
    request<Watchlist>("/api/v1/watchlists", {
      method: "POST",
      body: JSON.stringify({ name, symbols })
    }),
  rules: () => request<DetectionRule[]>("/api/v1/detection-rules"),
  createRule: (payload: { version: string; isActive: boolean; parametersJson: string }) =>
    request<DetectionRule>("/api/v1/detection-rules", {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  updateRule: (id: string, payload: { version: string; isActive: boolean; parametersJson: string }) =>
    request<DetectionRule>(`/api/v1/detection-rules/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }),
  health: () => request<SystemHealth>("/api/v1/system/health"),
  replay: (payload: { source: string; csvFile?: string; symbols?: string[] }) =>
    request<import("./types").ReplayResult>("/api/v1/replays", {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  backtests: () => request<import("./types").BacktestRun[]>("/api/v1/backtests"),
  runBacktest: (payload: { source: string; csvFile?: string; symbols?: string[] }) =>
    request<import("./types").BacktestRun>("/api/v1/backtests", {
      method: "POST",
      body: JSON.stringify(payload)
    })
};
