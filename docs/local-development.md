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

Replay CSV (`samples/demo-session.csv`, Twelve Data–shaped 1-minute bars) or PostgreSQL bars through the same `market.normalized` topic, or run an in-process backtest from **Replay / backtest**. Record rate is not a trading result.

```bash
curl -X POST http://localhost:5082/api/v1/backtests \
  -H "Content-Type: application/json" \
  -d '{"source":"csv","csvFile":"demo-session.csv","symbols":["AAPL","NVDA","TSLA"]}'
curl -X POST http://localhost:5082/api/v1/replays \
  -H "Content-Type: application/json" \
  -d '{"source":"csv","csvFile":"demo-session.csv"}'
```

Redpanda is `localhost:9092`. Redis is `localhost:6379`. PostgreSQL is `localhost:5432` (`marketpulse` / `marketpulse`). Kafka UI is `http://localhost:8080`. Prometheus is `http://localhost:9091`. Grafana is `http://localhost:3000` (admin/admin). API metrics: `http://localhost:5082/metrics`. Worker metrics: `http://localhost:9465/metrics`.

After ticks flow:

```bash
curl "http://localhost:5082/api/v1/anomalies?page=1&pageSize=20"
curl http://localhost:5082/api/v1/stocks/AAPL/snapshot
curl -X POST http://localhost:5082/api/v1/watchlists \
  -H "Content-Type: application/json" \
  -d '{"name":"core","symbols":["AAPL","NVDA"]}'
curl -X POST http://localhost:5082/api/v1/alert-configurations \
  -H "Content-Type: application/json" \
  -d '{"symbol":"*","minSeverity":"High","channel":"webhook","destination":"http://127.0.0.1:9/hook","isEnabled":true}'
```

Hub: `ws://localhost:5082/anomalyHub`. After reconnect, reload from REST. SignalR is not history.

Quiet markets may produce features without anomaly rows. Raise synthetic spike odds or wait for a high-RVOL print.

## Containers and CI

```bash
docker compose up -d
docker compose --profile app up --build
```

Web: http://localhost:8088. API inside the compose network is `api:8080`. Do not commit `TWELVE_DATA_API_KEY`; copy `.env.example` to `.env` if you need Twelve Data in the workers container.

```bash
docker build -f deploy/docker/Api.Dockerfile -t marketpulse-api:local .
docker build -f deploy/docker/Workers.Dockerfile -t marketpulse-workers:local .
docker build -f deploy/docker/Web.Dockerfile -t marketpulse-web:local .
kubectl apply -k deploy/k8s
```

Kubernetes is a single-replica demo. NodePort `30080` serves the SPA.

Switch ingestion to polling:

```bash
export MarketData__Mode=Polling
export MarketData__PollingIntervalSeconds=5
dotnet run --project src/MarketPulse.Workers
```
