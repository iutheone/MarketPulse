using MarketPulse.Application.Mapping;
using MarketPulse.Application.Replay;
using MarketPulse.Domain.Anomaly;
using MarketPulse.Domain.Entities;
using MarketPulse.Domain.Events;
using MarketPulse.Domain.Features;

namespace MarketPulse.UnitTests;

public class ReplayAndBacktestTests
{
    [Fact]
    public void CsvParser_SkipsLeadingComments()
    {
        const string csv = """
            # comment
            symbol,timestamp,open,high,low,close,volume
            AAPL,2026-01-15T14:00:00Z,1,1,1,1,10
            """;

        var bars = CsvMarketBarParser.Parse(csv);
        Assert.Single(bars);
        Assert.Equal("AAPL", bars[0].Symbol);
    }

    [Fact]
    public void CsvParser_ReadsBarsInTimestampOrder()
    {
        const string csv = """
            symbol,timestamp,open,high,low,close,volume
            AAPL,2026-01-15T14:01:00Z,1,1,1,1,10
            AAPL,2026-01-15T14:00:00Z,1,1,1,1,10
            """;

        var bars = CsvMarketBarParser.Parse(csv);
        Assert.Equal(2, bars.Count);
        Assert.True(bars[0].Timestamp < bars[1].Timestamp);
        Assert.Equal("csv", bars[0].Source);
        Assert.Equal("AAPL", bars[0].Symbol);
    }

    [Fact]
    public void Mapper_UsesSourceInDeterministicEventId()
    {
        var bar = new MarketBar
        {
            Symbol = "AAPL",
            Timestamp = new DateTimeOffset(2026, 1, 15, 14, 0, 0, TimeSpan.Zero),
            Open = 1, High = 1, Low = 1, Close = 1, Volume = 10,
            Source = "csv"
        };

        var first = MarketEventMapper.ToTick(bar);
        var second = MarketEventMapper.ToTick(bar);
        Assert.Equal(first.EventId, second.EventId);
        Assert.Equal("csv", first.Source);
    }

    [Fact]
    public async Task Replay_PublishesEveryBarOntoTheSameSink()
    {
        var csv = File.ReadAllText(FindSample());
        var bars = CsvMarketBarParser.Parse(csv);
        var publisher = new RecordingPublisher();
        foreach (var bar in bars)
        {
            await publisher.PublishAsync(MarketEventMapper.ToTick(bar), CancellationToken.None);
        }

        Assert.Equal(bars.Count, publisher.Ticks.Count);
        Assert.True(publisher.Ticks.Zip(publisher.Ticks.Skip(1), (a, b) => a.Timestamp <= b.Timestamp).All(x => x));
    }

    [Fact]
    public void Backtest_RecordsSpikeWithoutCallingItATradeResult()
    {
        var csv = File.ReadAllText(FindSample());
        var bars = CsvMarketBarParser.Parse(csv);
        var runner = new BacktestRunner(
            new FeatureCalculator(),
            new AnomalyDetectionEngine(new DetectionParameters()),
            TimeSpan.FromMinutes(60));

        var summary = Assert.Single(runner.Run(bars));
        Assert.Equal("AAPL", summary.Symbol);
        Assert.Equal(22, summary.Bars);
        Assert.True(summary.Anomalies >= 1);
        Assert.Contains("not", BacktestNotes.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("trading", BacktestNotes.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DemoSession_RecordsPlantedSpikes()
    {
        var csv = File.ReadAllText(FindSample("demo-session.csv"));
        var bars = CsvMarketBarParser.Parse(csv);
        Assert.Equal(200, bars.Count);
        Assert.Equal(["AAPL", "AMZN", "MSFT", "NVDA", "TSLA"], bars.Select(b => b.Symbol).Distinct().OrderBy(s => s).ToArray());

        var runner = new BacktestRunner(
            new FeatureCalculator(),
            new AnomalyDetectionEngine(new DetectionParameters()),
            TimeSpan.FromMinutes(60));
        var bySymbol = runner.Run(bars).ToDictionary(s => s.Symbol, StringComparer.OrdinalIgnoreCase);

        Assert.True(bySymbol["AAPL"].Anomalies >= 1);
        Assert.True(bySymbol["NVDA"].Anomalies >= 1);
        Assert.True(bySymbol["TSLA"].Anomalies >= 1);
        Assert.True(bySymbol["AAPL"].Anomalies > bySymbol["AMZN"].Anomalies);
    }

    private static string FindSample(string fileName = "replay-aapl.csv")
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "samples", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", fileName));
    }

    private sealed class RecordingPublisher : MarketPulse.Application.Interfaces.IMarketEventPublisher
    {
        public List<MarketTick> Ticks { get; } = [];

        public Task EnsureInfrastructureAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task PublishAsync(MarketTick marketTick, CancellationToken cancellationToken)
        {
            Ticks.Add(marketTick);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
