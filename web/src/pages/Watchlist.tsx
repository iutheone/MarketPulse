import { FormEvent, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";
import type { Watchlist } from "../types";
import { Disclaimer, EmptyState, PageHeader, formatTime } from "../ui";

export function WatchlistPage() {
  const [lists, setLists] = useState<Watchlist[]>([]);
  const [name, setName] = useState("Core");
  const [symbols, setSymbols] = useState("AAPL, MSFT, NVDA");
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setLists(await api.watchlists());
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load watchlists");
    }
  }

  useEffect(() => {
    void load();
  }, []);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    const parsed = symbols
      .split(/[\s,]+/)
      .map((item) => item.trim().toUpperCase())
      .filter(Boolean);
    try {
      await api.createWatchlist(name, parsed);
      setName("");
      setSymbols("");
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Create failed");
    }
  }

  return (
    <section>
      <PageHeader kicker="Universe" title="Watchlists">
        <p className="lede">Named symbol groups stored in PostgreSQL. They do not drive ingestion yet.</p>
      </PageHeader>
      <Disclaimer />
      {error ? <p className="error">{error}</p> : null}
      <form className="stack" onSubmit={(event) => void onSubmit(event)}>
        <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Name" required />
        <input
          value={symbols}
          onChange={(event) => setSymbols(event.target.value)}
          placeholder="Symbols, comma separated"
          required
        />
        <button className="primary" type="submit">
          Save list
        </button>
      </form>
      {lists.length === 0 ? <EmptyState>No lists yet.</EmptyState> : null}
      <div className="list">
        {lists.map((list) => (
          <article className="card" key={list.id}>
            <strong>{list.name}</strong>
            <div className="chip-row">
              {list.symbols.map((symbol) => (
                <Link className="chip" key={symbol} to={`/stocks/${symbol}`}>
                  {symbol}
                </Link>
              ))}
            </div>
            <p className="muted">{formatTime(list.createdAt)}</p>
          </article>
        ))}
      </div>
    </section>
  );
}
