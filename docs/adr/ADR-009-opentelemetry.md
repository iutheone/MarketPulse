# ADR-009: OpenTelemetry metrics and traces

## Status

Accepted (Phase 9)

## Context

JSON logs and `/health/*` are not enough to see pipeline rate, feature latency, or alert failures across processes.

## Decision

`System.Diagnostics.Metrics` / `ActivitySource` named `MarketPulse` live in Application. API exposes Prometheus at `/metrics`. Workers listen on port 9465. Docker Compose runs Prometheus (9091) and Grafana (3000). Optional OTLP is off unless `Observability:OtlpEndpoint` is set.

Pipeline counters are engineering telemetry (ticks, recorded anomalies, webhook outcomes). They are not trading statistics.

## Consequences

- Local scrape uses `host.docker.internal` so Prometheus in Docker can reach processes on the host.
- High-cardinality tags (per symbol) are kept on spans, not on counters.
