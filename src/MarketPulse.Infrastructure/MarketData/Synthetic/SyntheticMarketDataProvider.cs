using System.Runtime.CompilerServices;
using MarketPulse.Application.Options;
using MarketPulse.Domain.Entities;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.MarketData.Synthetic;

/// <summary>
/// Local, license-free market source. Used for Phase 1 so Kafka ingestion can be demonstrated
/// without Twelve Data. Same IMarketDataProvider contract as future REST/WebSocket adapters.
/// </summary>
public sealed class SyntheticMarketDataProvider : IMarketDataProvider
{
    private readonly SyntheticMarketDataOptions _options;
    private readonly TimeProvider _clock;
    private readonly Dictionary<string, SymbolBook> _books = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private int _tickIndex;

    public SyntheticMarketDataProvider(IOptions<SyntheticMarketDataOptions> options, TimeProvider? clock = null)
    {
        _options = options.Value;
        _clock = clock ?? TimeProvider.System;
        foreach (var symbol in ResolveSymbols())
        {
            _books[symbol] = SymbolBook.Create(symbol);
        }
    }

    public Task<IReadOnlyList<MarketBar>> GetLatestAsync(
        IEnumerable<string> symbols,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ticks = NextBatch(symbols);
        IReadOnlyList<MarketBar> bars = ticks.Select(ToBar).ToList();
        return Task.FromResult(bars);
    }

    public async IAsyncEnumerable<MarketTick> StreamAsync(
        IEnumerable<string> symbols,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromMilliseconds(Math.Max(50, _options.IntervalMilliseconds));
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var tick in NextBatch(symbols))
            {
                yield return tick;
            }

            try
            {
                await Task.Delay(delay, _clock, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
        }
    }

    internal IReadOnlyList<MarketTick> NextBatch(IEnumerable<string> symbols)
    {
        lock (_gate)
        {
            _tickIndex++;
            var requested = symbols.Select(s => s.ToUpperInvariant()).Distinct().ToArray();
            if (requested.Length == 0)
            {
                requested = ResolveSymbols();
            }

            var ticks = new List<MarketTick>(requested.Length);
            foreach (var symbol in requested)
            {
                if (!_books.TryGetValue(symbol, out var book))
                {
                    book = SymbolBook.Create(symbol);
                    _books[symbol] = book;
                }

                ticks.Add(Advance(book, _tickIndex));
            }

            return ticks;
        }
    }

    private MarketTick Advance(SymbolBook book, int tickIndex)
    {
        var rng = CreateRng(book.Symbol, tickIndex);
        var open = book.LastClose;

        var drift = (decimal)((rng.NextDouble() - 0.48) * 0.004);
        var close = decimal.Round(open * (1 + drift), 4);

        if (rng.NextDouble() < _options.PriceSpikeProbability)
        {
            var jump = (decimal)(0.012 + rng.NextDouble() * 0.02);
            close = decimal.Round(open * (1 + (rng.NextDouble() < 0.5 ? -jump : jump)), 4);
        }

        var spread = decimal.Round(Math.Abs(close - open) + (decimal)(rng.NextDouble() * 0.15), 4);
        var high = Math.Max(open, close) + spread;
        var low = Math.Max(0.01m, Math.Min(open, close) - spread);

        var acceleration = 1m + (decimal)(Math.Sin(tickIndex / 25.0) * 0.15 + tickIndex * 0.002);
        var volume = (long)(book.BaseVolume * (0.85 + rng.NextDouble() * 0.3) * (double)acceleration);
        if (rng.NextDouble() < _options.VolumeSpikeProbability)
        {
            volume = (long)(volume * (5 + rng.NextDouble() * 5));
        }

        book.LastClose = close;

        return new MarketTick
        {
            EventId = NewDeterministicEventId(book.Symbol, tickIndex),
            Symbol = book.Symbol,
            Timestamp = _clock.GetUtcNow(),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = Math.Max(1, volume),
            Source = "synthetic",
            SchemaVersion = SchemaVersions.MarketTick
        };
    }

    private Random CreateRng(string symbol, int tickIndex)
    {
        var seed = _options.Seed ?? Environment.TickCount;
        return new Random(HashCode.Combine(seed, symbol, tickIndex));
    }

    private Guid NewDeterministicEventId(string symbol, int tickIndex)
    {
        var seed = _options.Seed ?? 0;
        var bytes = new byte[16];
        BitConverter.GetBytes(HashCode.Combine(seed, symbol)).CopyTo(bytes, 0);
        BitConverter.GetBytes(tickIndex).CopyTo(bytes, 8);
        return new Guid(bytes);
    }

    private string[] ResolveSymbols()
    {
        return _options.Symbols.Length == 0
            ? ["AAPL", "MSFT", "NVDA", "TSLA", "AMZN"]
            : _options.Symbols.Select(s => s.ToUpperInvariant()).ToArray();
    }

    private static MarketBar ToBar(MarketTick tick)
    {
        return new MarketBar
        {
            Symbol = tick.Symbol,
            Timestamp = tick.Timestamp,
            Open = tick.Open,
            High = tick.High,
            Low = tick.Low,
            Close = tick.Close,
            Volume = tick.Volume,
            Source = tick.Source
        };
    }

    private sealed class SymbolBook
    {
        public required string Symbol { get; init; }
        public required decimal LastClose { get; set; }
        public required long BaseVolume { get; init; }

        public static SymbolBook Create(string symbol)
        {
            var (price, volume) = symbol.ToUpperInvariant() switch
            {
                "AAPL" => (190.15m, 48_000L),
                "MSFT" => (418.40m, 32_000L),
                "NVDA" => (124.80m, 55_000L),
                "TSLA" => (248.10m, 61_000L),
                "AMZN" => (182.55m, 40_000L),
                "GOOGL" => (165.20m, 28_000L),
                "META" => (511.75m, 22_000L),
                _ => (100m, 20_000L)
            };

            return new SymbolBook
            {
                Symbol = symbol.ToUpperInvariant(),
                LastClose = price,
                BaseVolume = volume
            };
        }
    }
}
