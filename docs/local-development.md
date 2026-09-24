# Local development

Set the Twelve Data key (never commit it):

```bash
dotnet user-secrets set "TwelveData:ApiKey" "<your-key>" --project src/MarketPulse.Workers
```

Then:

```bash
docker compose up -d
dotnet test
dotnet run --project src/MarketPulse.Workers
dotnet run --project src/MarketPulse.Api
cd web && npm install && npm run dev
```

Replay CSV (`samples/replay-aapl.csv`) or PostgreSQL bars through the same `market.normalized` topic, or run an in-process backtest from **Replay / backtest**. Record rate is not a trading result.

```bash
curl -X POST http://localhost:5082/api/v1/backtests \
  -H "Content-Type: application/json" \
  -d '{"source":"csv","csvFile":"replay-aapl.csv","symbols":["AAPL"]}'
```

Redpanda is `localhost:9092`. Redis is `localhost:6379`. PostgreSQL is `localhost:5432` (`marketpulse` / `marketpulse`). Kafka UI is `http://localhost:8080`.

After ticks flow:

```bash
curl "http://localhost:5082/api/v1/anomalies?page=1&pageSize=20"
curl http://localhost:5082/api/v1/stocks/AAPL/snapshot
curl -X POST http://localhost:5082/api/v1/watchlists \
  -H "Content-Type: application/json" \
  -d '{"name":"core","symbols":["AAPL","NVDA"]}'
```

Hub: `ws://localhost:5082/anomalyHub`. After reconnect, reload from REST. SignalR is not history.

Quiet markets may produce features without anomaly rows. Raise synthetic spike odds or wait for a high-RVOL print.

Switch ingestion to polling:

```bash
export MarketData__Mode=Polling
export MarketData__PollingIntervalSeconds=5
dotnet run --project src/MarketPulse.Workers
```
