export type Anomaly = {
  id: string;
  sourceEventId: string;
  symbol: string;
  detectedAt: string;
  score: number;
  severity: string;
  reasons: string[];
  ruleVersion: string;
  relativeVolume: number | null;
  volumeAcceleration: number | null;
  priceChangePercent: number;
  vwapDeviationPercent: number;
  breakout: boolean;
  lastPrice: number;
  volume1m: number;
};

export type AnomalyLive = {
  id: string;
  symbol: string;
  score: number;
  severity: string;
  timestamp: string;
  reasons: string[];
};

export type Feature = {
  symbol: string;
  sourceEventId: string;
  calculatedAt: string;
  lastPrice: number;
  lastVolume: number;
  volume1m: number;
  volume5m: number;
  volume15m: number;
  baselineVolumePerMinute: number;
  relativeVolume: number | null;
  volumeAcceleration: number | null;
  priceChange: number;
  priceChangePercent: number;
  vwap: number;
  vwapDeviationPercent: number;
  breakoutHigh: boolean;
  breakoutLow: boolean;
  volatility: number;
  sampleCount: number;
  hasSufficientHistory: boolean;
};

export type Bar = {
  eventId: string;
  symbol: string;
  timestamp: string;
  open: number;
  high: number;
  low: number;
  close: number;
  volume: number;
  source: string;
};

export type Snapshot = {
  symbol: string;
  latestBar: Bar | null;
  features: Feature | null;
  latestAnomaly: Anomaly | null;
  note: string;
};

export type Paged<T> = {
  page: number;
  pageSize: number;
  total: number;
  items: T[];
};

export type Watchlist = {
  id: string;
  name: string;
  symbols: string[];
  createdAt: string;
};

export type DetectionRule = {
  id: string;
  version: string;
  isActive: boolean;
  parametersJson: string;
  createdAt: string;
};

export type SystemHealth = {
  status: string;
  totalDurationMs: number;
  checks: { name: string; status: string; description?: string | null; durationMs: number }[];
};

export type ReplayResult = {
  barsRead: number;
  published: number;
  source: string;
  note: string;
};

export type BacktestResult = {
  id: string;
  symbol: string;
  bars: number;
  anomalies: number;
  recordRate: number;
  meanScore: number | null;
  notes: string;
};

export type BacktestRun = {
  id: string;
  startedAt: string;
  completedAt?: string | null;
  status: string;
  parametersJson: string;
  results: BacktestResult[];
  error?: string | null;
};
