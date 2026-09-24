# Redis hot state

Redis holds rolling windows and the latest feature snapshot. **PostgreSQL is the durable source of truth**. Flushing Redis only loses hot state; Kafka can rebuild it.

## Keys

| Key | Structure | Why |
| --- | --- | --- |
| `market:{symbol}:window` | Sorted set. Score = event-time Unix ms. Member = `eventId\|ts\|o\|h\|l\|c\|v` | Range-delete old ticks with `ZREMRANGEBYSCORE`. Same `eventId` member makes duplicate Kafka deliveries a no-op. |
| `market:{symbol}:latest` | String (JSON tick) | O(1) snapshot for dashboards without decoding the window. |
| `market:{symbol}:features` | String (JSON `MarketFeatures`) | One read for the API. Recomputed on every tick; not a ledger. |
| `market:{symbol}:volume:1m` / `5m` / `15m` | String (integer) | Cheap `GET` for operators (`redis-cli`) without JSON. |
| `market:{symbol}:vwap` | Hash (`value`, `deviationPct`, `eventId`) | Field-level reads of VWAP without the full feature blob. |
| `market:anomalies:latest` | List (JSON `AnomalyResult`, cap 100) | Operator peek. History lives in Postgres. |
| `market:{symbol}:anomaly:latest` | String (JSON) | Last recorded anomaly for a symbol. |

## What we do not do

- Query PostgreSQL on each market event for feature math.
- Treat Redis as history. Retention is `FeatureProcessing:RetentionMinutes` (default 60).

## Inspect locally

```bash
redis-cli GET market:AAPL:features
redis-cli GET market:AAPL:volume:1m
redis-cli HGETALL market:AAPL:vwap
redis-cli ZCARD market:AAPL:window
redis-cli LRANGE market:anomalies:latest 0 4
```
