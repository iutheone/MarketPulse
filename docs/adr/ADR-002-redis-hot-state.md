# ADR-002 Redis for hot state

## Status

Accepted (Phase 2)

## Context

Feature windows (1m/5m/15m volume, VWAP) must update on every normalized tick. Hitting PostgreSQL per event would add latency and load that we do not need for ephemeral rolling state.

## Decision

Store rolling tick samples and the latest `MarketFeatures` in Redis. Feature math runs in process (`FeatureCalculator`) after reading the symbol window. Kafka `market.normalized` remains the replay source if Redis is flushed.

## Consequences

- Feature processor scales with Kafka partitions (one in-order stream per symbol key).
- Redis loss is recoverable by replaying the topic (consumer group reset).
- Anomaly persistence still belongs in PostgreSQL (Phase 3).
