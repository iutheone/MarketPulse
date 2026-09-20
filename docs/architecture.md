# Architecture

```
Synthetic / future providers
        ↓
MarketDataIngestionWorker
        ↓
market.normalized   (Kafka, key = symbol)
        ↓
FeatureProcessorWorker
        ↓
FeatureCalculator (event-time windows)
        ↓
Redis hot state   (not durable history)
```

Phase 2 does **not** write anomalies or PostgreSQL.

## Feature processor

- Consumes `market.normalized` with `EnableAutoCommit = false`.
- Commits the offset only after Redis features are written.
- Windows use **event time** on the tick, so historical replay later will compute the same numbers.
- Duplicate `EventId` members in the sorted set are idempotent.

## Failure

- Kafka down: both workers retry topology/bootstrap.
- Redis down: process throws, offset is not committed, Kafka redelivers.
- Unreadable JSON: logged and committed so one poison payload does not stall a partition (Phase 3 will route these to `alerts.dlq`).

## Scale

- Add feature-processor instances up to the partition count. Each partition is consumed by one group member.
- Redis is a single logical hot store; shard later only if one instance cannot hold the 60-minute windows.
