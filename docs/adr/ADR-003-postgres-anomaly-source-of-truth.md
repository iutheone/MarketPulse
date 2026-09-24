# ADR-003: PostgreSQL is the source of truth for anomalies

## Status

Accepted (Phase 3)

## Context

Redis already holds rolling windows and the latest feature snapshot. Those keys are cheap to rebuild from Kafka. Anomaly records, bars, and “did we already process this EventId?” need to survive a Redis flush and a worker restart.

## Decision

PostgreSQL is the durable store. The feature processor, after writing Redis features, runs the detection engine in-process and then:

1. Inserts `ProcessedEvents.EventId` (unique). A conflict means a Kafka redelivery; skip publish.
2. Writes `MarketEvents` / `MarketBars` in the same transaction.
3. Writes `Anomalies` only when the score is at or above `AnomalyDetection:MinScoreToRecord`.
4. Publishes `market.anomalies` (key = symbol) **after** the transaction commits.
5. Pushes a short Redis list `market:anomalies:latest` for operators; Redis is still not history.

The score is a weighted engineering metric (0–100), not a probability and not a trade recommendation.

## Consequences

`FeatureProcessor` and `DbContext` are scoped per Kafka message. The Kafka consumer stays a singleton and opens a scope, then commits the offset only if persist succeeds.
