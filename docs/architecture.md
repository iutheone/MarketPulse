# Architecture

```
Provider
  ↓
market.normalized
  ↓
FeatureProcessor (Redis) + AnomalyDetectionEngine
  ↓
PostgreSQL  then  market.anomalies
                    ↓
           API realtime consumer
                    ↓
              SignalR /anomalyHub
                    ↓
              React (web/) + REST /api/v1
```

The anomaly **score is not a probability and not a trade signal**.

## Replay and backtest

CSV (`samples/*.csv`) or PostgreSQL `MarketBars` become `MarketTick`s with `source=csv` (or the stored bar source). Replay publishes to **`market.normalized`**. Downstream feature and anomaly processors cannot tell live from historical except by `source` and EventId.

In-process backtests walk `FeatureCalculator` + `AnomalyDetectionEngine` without Kafka. Stored `HitRate` is **record rate** (anomalies / bars), not forecast accuracy. `MarketData:Provider=Replay` runs the sample CSV once on worker start instead of live ingestion.

## Query API

Controllers return DTOs only. History and anomaly lists page from PostgreSQL. Snapshots and features read Redis first and attach the latest Postgres anomaly when one exists.

Watchlists and detection-rule catalog rows live in PostgreSQL. Changing a rule row does not hot-reload the worker engine in this phase.

## Realtime

- Separate Kafka group `marketpulse-api-anomalies`, `AutoOffsetReset = Latest`.
- Offset commits after the hub send.
- Redis SignalR backplane so extra API replicas share connections.
- Missed messages after a disconnect: REST, not the hub.

## Failure

- Kafka down: workers retry; API ready-check fails; REST still answers from Postgres if the DB is up.
- Redis down: features/backplane fail; history queries still work.
- Unreadable Kafka JSON: logged and committed so a poison payload does not stall the hub consumer.
