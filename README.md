# MarketPulse

Real-time market-data **anomaly detection** platform (engineering/analytics). An anomaly score describes unusual volume/price behavior. It is **not** a price forecast and **not** a buy/sell recommendation.

Current milestone: **Phase 1 — synthetic ingestion → Kafka `market.normalized`**.

```
SyntheticMarketDataProvider
        ↓
MarketDataIngestionWorker
        ↓
KafkaMarketEventPublisher  (key = symbol)
        ↓
market.normalized
```

## Prerequisites

- .NET 8 SDK
- Docker Desktop

## Run Phase 1

```bash
docker compose up -d
dotnet test MarketPulse.sln
dotnet run --project src/MarketPulse.Workers
```

Optional health API (does not ingest data):

```bash
dotnet run --project src/MarketPulse.Api
```

- Worker logs JSON lines: `Published market event {EventId} for {Symbol} to market.normalized ...`
- Kafka UI: http://localhost:8080 — topic `market.normalized`
- Live: http://localhost:5082/health/live
- Ready: http://localhost:5082/health/ready (fails if Kafka is down)

## Configuration

`src/MarketPulse.Workers/appsettings.json`

| Key | Meaning |
| --- | --- |
| `MarketData:Provider` | Phase 1: `Synthetic` only |
| `MarketData:Mode` | `Streaming` or `Polling` |
| `MarketData:PollingIntervalSeconds` | Poll cadence (not hardcoded to 60s) |
| `SyntheticMarketData:IntervalMilliseconds` | Streaming tick interval |
| `SyntheticMarketData:Seed` | Deterministic generator seed |
| `Kafka:BootstrapServers` | Default `localhost:9092` |

Override with environment variables, e.g. `Kafka__BootstrapServers`, `MarketData__Mode=Polling`.

## Solution

| Project | Responsibility |
| --- | --- |
| `MarketPulse.Domain` | `MarketTick`, `MarketBar`, `IMarketDataProvider` |
| `MarketPulse.Application` | Options, mapper, Kafka ports (no Confluent types) |
| `MarketPulse.Infrastructure` | Synthetic provider, Kafka producer, health check |
| `MarketPulse.Workers` | Ingestion background service |
| `MarketPulse.Api` | Liveness/readiness (Phase 4 adds REST/SignalR) |

Twelve Data, Redis, PostgreSQL, React, and anomaly scoring are **intentionally absent** until later phases.

## Docs

- [docs/architecture.md](docs/architecture.md)
- [docs/local-development.md](docs/local-development.md)
- [docs/adr/ADR-001-kafka-event-backbone.md](docs/adr/ADR-001-kafka-event-backbone.md)
- [docs/adr/ADR-004-provider-abstraction.md](docs/adr/ADR-004-provider-abstraction.md)
- [docs/adr/ADR-005-symbol-kafka-partitioning.md](docs/adr/ADR-005-symbol-kafka-partitioning.md)
