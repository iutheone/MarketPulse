# ADR-006: SignalR is a fan-out, not a store

## Status

Accepted (Phase 4)

## Context

The dashboard needs live anomaly rows. Kafka already has `market.anomalies`. PostgreSQL already has history. Putting a second durable log inside SignalR would duplicate both.

## Decision

The API consumes `market.anomalies` with its own consumer group (`marketpulse-api-anomalies`, `AutoOffsetReset = Latest`). Each message is mapped to a DTO and sent to `/anomalyHub` (`anomalyDetected`). Clients join group `all` or a symbol.

SignalR uses the existing Redis instance as a backplane so more than one API replica can share connections. A dropped websocket is not replayed from the hub. On reconnect, the client must call REST (`GET /api/v1/anomalies`, snapshot) to catch up.

REST never returns EF entities.

## Consequences

If Redis is down, the backplane fails and the API is not ready. Feature math still uses Redis independently. Live lag is acceptable; missed ticks during a disconnect are recovered from Postgres.
