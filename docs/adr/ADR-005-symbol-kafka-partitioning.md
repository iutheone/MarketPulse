# ADR-005 Symbol-based Kafka partitioning

## Status

Accepted (Phase 1)

## Context

Rolling volume/VWAP for a symbol is easier if events for that symbol are totally ordered.

## Decision

The Kafka message **key** is `MarketTick.Symbol`. Kafka hashes the key to a partition. `Kafka:NormalizedTopicPartitions` defaults to 6.

## Consequences

- Ordering is guaranteed per partition, hence per symbol (assuming the key stays the symbol).
- Global ordering across symbols is not provided.
- Changing partition count later requires a new topic or a migration; do not change it casually in production.
