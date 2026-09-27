# ADR-008: Alerting uses Kafka outbox, not the scoring path

## Status

Accepted (Phase 8)

## Context

Webhook HTTP is slow and unreliable. Calling it from `AnomalyProcessor` would stall `market.normalized` offset commits.

## Decision

Recorded anomalies still publish only to `market.anomalies`. A dedicated dispatcher (group `marketpulse-alert-dispatcher`, `AutoOffsetReset = Latest`) matches enabled webhook configs, inserts `AlertDeliveries` with a unique `(AnomalyId, ConfigurationId)`, and produces `alerts.outbox`. A delivery worker POSTs the webhook, retries, then `alerts.dlq`.

Payloads include a disclaimer: scores are unusual-activity metrics, not trade advice.

## Consequences

- Duplicate Kafka deliveries do not double-fire (unique row).
- Historical anomalies at first deploy do not backfill webhooks (`Latest`).
- Failed destinations are visible in `AlertDeliveries` and `alerts.dlq`.
