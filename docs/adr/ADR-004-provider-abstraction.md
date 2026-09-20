# ADR-004 Provider abstraction

## Status

Accepted (Phase 1)

## Context

Twelve Data is one possible source. Tight coupling would force the pipeline to change for replay, synthetic load tests, or another vendor.

## Decision

`IMarketDataProvider` in Domain defines `GetLatestAsync` (polling) and `StreamAsync` (websocket/push). Infrastructure implements `SyntheticMarketDataProvider`. Twelve Data REST/WebSocket adapters must map vendor DTOs to `MarketBar` / `MarketTick` inside the adapter.

## Consequences

- Ingestion worker and Kafka publisher stay vendor-agnostic.
- Phase 1 fails fast if `MarketData:Provider` is `TwelveData`.
- Phase 6 implements `TwelveDataRestProvider`; WebSocket remains a future adapter.
- Historical replay (Phase 7) publishes the same canonical events.
