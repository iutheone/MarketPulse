# MarketPulse

Real-time market-data **anomaly detection** platform (engineering/analytics). An anomaly score describes unusual volume/price behavior. It is **not** a price forecast and **not** a buy/sell recommendation.

Current milestone: **Phase 7 — historical replay and backtesting**.

```
Provider / CSV replay
        ↓
market.normalized
        ↓
FeatureProcessor (Redis) + AnomalyDetectionEngine
        ↓
PostgreSQL  then  market.anomalies → SignalR /anomalyHub → React
```

## Prerequisites

- .NET 8 SDK
- Docker Desktop
- Node 20+ (dashboard)

## Twelve Data

Do **not** put the API key in `appsettings.json` or git.

```bash
dotnet user-secrets set "TwelveData:ApiKey" "<your-key>" --project src/MarketPulse.Workers
```

Or: `export TwelveData__ApiKey=...`

`MarketData:Provider` is `TwelveData` or `Synthetic`. Set `Replay` to publish `samples/replay-aapl.csv` once instead of live ingestion.

## Run locally

```bash
docker compose up -d
dotnet test MarketPulse.sln
dotnet run --project src/MarketPulse.Workers
dotnet run --project src/MarketPulse.Api
cd web && npm install && npm run dev
```

Open http://localhost:5173 — **Replay / backtest** runs the sample CSV in process, or publishes it to Kafka.

Backtest **record rate** is anomalies/bars for the current rule set, not a trading hit rate.

## Configuration

`src/MarketPulse.Workers/appsettings.json`

| Key | Meaning |
| --- | --- |
| `MarketData:Provider` | `Synthetic`, `TwelveData`, or `Replay` |
| `MarketData:Mode` | `Streaming` or `Polling` (Twelve Data forces polling) |
| `Replay:CsvFile` | File under `samples/` when Provider is Replay |
| `TwelveData:ApiKey` | User secret / `TwelveData__ApiKey` — never commit |
| `Kafka:BootstrapServers` | Default `localhost:9092` |
| `Redis:ConnectionString` | Default `localhost:6379` |
| `Postgres:ConnectionString` | Default local `marketpulse` |

## Solution

| Project | Responsibility |
| --- | --- |
| `MarketPulse.Domain` | Ticks, features, anomaly scoring |
| `MarketPulse.Application` | Ports, processors, replay/backtest |
| `MarketPulse.Infrastructure` | Twelve Data, Kafka, Redis, Postgres |
| `MarketPulse.Workers` | Ingestion (or replay) + feature consumer |
| `MarketPulse.Api` | REST + SignalR |
| `web/` | React dashboard |

## Docs

- [docs/architecture.md](docs/architecture.md)
- [docs/redis-design.md](docs/redis-design.md)
- [docs/local-development.md](docs/local-development.md)
- [docs/adr/ADR-001-kafka-event-backbone.md](docs/adr/ADR-001-kafka-event-backbone.md)
- [docs/adr/ADR-002-redis-hot-state.md](docs/adr/ADR-002-redis-hot-state.md)
- [docs/adr/ADR-003-postgres-anomaly-source-of-truth.md](docs/adr/ADR-003-postgres-anomaly-source-of-truth.md)
- [docs/adr/ADR-004-provider-abstraction.md](docs/adr/ADR-004-provider-abstraction.md)
- [docs/adr/ADR-005-symbol-kafka-partitioning.md](docs/adr/ADR-005-symbol-kafka-partitioning.md)
- [docs/adr/ADR-006-signalr-fanout.md](docs/adr/ADR-006-signalr-fanout.md)
- [docs/adr/ADR-007-historical-replay.md](docs/adr/ADR-007-historical-replay.md)
