# Demo market data

These files stand in for Twelve Data `time_series` 1-minute bars. They are **not** live quotes and **not** a trading feed. No API key is required.

| File | Use |
| --- | --- |
| `demo-session.csv` | Five symbols, 40 minutes, with planted volume/price events for the dashboard |
| `replay-aapl.csv` | Tiny AAPL spike used by unit tests |
| `twelvedata/time_series_*.json` | Vendor JSON shape (`meta`, `values` newest-first, string OHLC) |

Replay or backtest from the UI with `csvFile: demo-session.csv`. Source on Kafka ticks remains `csv` after replay.
