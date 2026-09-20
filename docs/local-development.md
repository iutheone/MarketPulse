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
```

Redpanda is `localhost:9092`. Redis is `localhost:6379`. Kafka UI is `http://localhost:8080`.

After ticks flow:

```bash
redis-cli GET market:AAPL:features
redis-cli GET market:AAPL:volume:1m
```

Optional API (port 5082):

```bash
dotnet run --project src/MarketPulse.Api
curl http://localhost:5082/api/v1/stocks/AAPL/features
```

Switch ingestion to polling:

```bash
export MarketData__Mode=Polling
export MarketData__PollingIntervalSeconds=5
dotnet run --project src/MarketPulse.Workers
```
