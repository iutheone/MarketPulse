# ADR-001 Kafka as event backbone

## Status

Accepted (Phase 1)

## Context

Ingestion, feature processing, anomaly detection, and alerting must scale independently and survive process restarts.

## Decision

All normalized market events go to Kafka topic `market.normalized`. Downstream services consume that topic. Direct method calls between ingestion and processors are not used.

## Consequences

- Producers and consumers can be deployed separately.
- At-least-once delivery requires idempotency in later phases (`EventId`).
- Local development needs Docker (Redpanda) even for a single-machine demo.
