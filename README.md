# MarketPulse

Real-time market-data **anomaly detection** platform (engineering/analytics). An anomaly score describes unusual volume/price behavior. It is **not** a price forecast and **not** a buy/sell recommendation.

Current milestone: **Phase 6 (partial) — Twelve Data REST polling into the same Kafka pipeline**.

```
TwelveDataRestProvider  (or Synthetic)
        ↓  GetLatestAsync
MarketDataIngestionWorker  (Polling)
        ↓
market.normalized
        ↓
FeatureProcessorWorker → Redis
```

## Prerequisites

- .NET 8 SDK
- Docker Desktop

## Twelve Data

Do **not** put the API key in `appsettings.json` or git.

```bash
dotnet user-secrets set "TwelveData:ApiKey" "<your-key>" --project src/MarketPulse.Workers
```

Or: `export TwelveData__ApiKey=...`

`MarketData:Provider` is `TwelveData` and `Mode` is `Polling` (REST cannot stream). Interval is `TwelveData:Interval` (`1min`). A 429 waits `RateLimitRetrySeconds` and retries that symbol once.

Switch back to synthetic: `"Provider": "Synthetic"`.

```bash
docker compose up -d
dotnet test MarketPulse.sln
dotnet run --project src/MarketPulse.Workers
```

The worker hosts **both** ingestion and the feature processor.

Inspect features:

```bash
redis-cli GET market:AAPL:features
curl http://localhost:5082/api/v1/stocks/AAPL/features
```

Health API:

```bash
dotnet run --project src/MarketPulse.Api
```

- Kafka UI: http://localhost:8080 — topic `market.normalized`
- Live: http://localhost:5082/health/live
- Ready: http://localhost:5082/health/ready (Kafka **and** Redis)

## Configuration

`src/MarketPulse.Workers/appsettings.json`

| Key | Meaning |
| --- | --- |
| `MarketData:Provider` | `Synthetic` or `TwelveData` |
| `MarketData:Mode` | `Streaming` or `Polling` (Twelve Data forces polling) |
| `MarketData:PollingIntervalSeconds` | Poll cadence |
| `TwelveData:ApiKey` | User secret / `TwelveData__ApiKey` — never commit |
| `SyntheticMarketData:IntervalMilliseconds` | Streaming tick interval |
| `Kafka:BootstrapServers` | Default `localhost:9092` |
| `Kafka:FeatureProcessorGroupId` | Consumer group for features |
| `Redis:ConnectionString` | Default `localhost:6379` |
| `FeatureProcessing:RetentionMinutes` | Rolling window kept in Redis |

## Solution

| Project | Responsibility |
| --- | --- |
| `MarketPulse.Domain` | Ticks, `FeatureCalculator` |
| `MarketPulse.Application` | Ports, `FeatureProcessor` |
| `MarketPulse.Infrastructure` | Synthetic + Twelve Data REST, Kafka, Redis |
| `MarketPulse.Workers` | Ingestion + feature consumer |
| `MarketPulse.Api` | Health + feature snapshot read |

PostgreSQL, anomaly scoring, SignalR, React, and Twelve Data WebSocket are still later work.

## Docs

- [docs/architecture.md](docs/architecture.md)
- [docs/redis-design.md](docs/redis-design.md)
- [docs/local-development.md](docs/local-development.md)
- [docs/adr/ADR-001-kafka-event-backbone.md](docs/adr/ADR-001-kafka-event-backbone.md)
- [docs/adr/ADR-002-redis-hot-state.md](docs/adr/ADR-002-redis-hot-state.md)
- [docs/adr/ADR-004-provider-abstraction.md](docs/adr/ADR-004-provider-abstraction.md)
- [docs/adr/ADR-005-symbol-kafka-partitioning.md](docs/adr/ADR-005-symbol-kafka-partitioning.md)
