# ADR-007: Replay uses the live Kafka topic

## Status

Accepted (Phase 7)

## Context

Historical ticks must exercise the same feature and anomaly processors as live data. A private replay topic would hide bugs that only show up on `market.normalized`.

## Decision

`HistoricalReplayService` maps CSV or PostgreSQL bars to `MarketTick` and publishes to `market.normalized` with `key = symbol`. EventId stays `SHA256(source|symbol|timestamp)`. CSV uses `source=csv`.

Backtests do **not** publish. They walk `FeatureCalculator` and `AnomalyDetectionEngine` in process and store `BacktestRuns` / `BacktestResults`. `HitRate` on the row is **record rate** (anomalies / bars), not forecast accuracy and not a trading result.

## Consequences

Replaying bars that already have the same EventId is a no-op in `ProcessedEvents`. Use a CSV (`source=csv`) to inject a new series. Setting `MarketData:Provider` to `Replay` runs the CSV once on worker start instead of live ingestion.
