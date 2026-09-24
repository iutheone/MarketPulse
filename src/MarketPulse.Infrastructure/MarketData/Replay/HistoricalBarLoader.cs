using MarketPulse.Application.Contracts;
using MarketPulse.Application.Interfaces;
using MarketPulse.Application.Options;
using MarketPulse.Application.Replay;
using MarketPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MarketPulse.Infrastructure.MarketData.Replay;

public sealed class HistoricalBarLoader : IHistoricalBarLoader
{
    private readonly ReplayOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly MarketPulse.Infrastructure.Persistence.MarketPulseDbContext _db;

    public HistoricalBarLoader(
        IOptions<ReplayOptions> options,
        IHostEnvironment environment,
        Persistence.MarketPulseDbContext db)
    {
        _options = options.Value;
        _environment = environment;
        _db = db;
    }

    public async Task<IReadOnlyList<MarketBar>> LoadAsync(ReplayRequest request, CancellationToken cancellationToken)
    {
        var source = (request.Source ?? "csv").Trim().ToLowerInvariant();
        var bars = source switch
        {
            "postgres" => await LoadPostgresAsync(request, cancellationToken),
            "csv" => LoadCsv(request),
            _ => throw new InvalidOperationException("Replay source must be csv or postgres.")
        };

        var symbols = request.Symbols.Select(s => s.Trim().ToUpperInvariant()).Where(s => s.Length > 0).ToHashSet();
        if (symbols.Count > 0)
        {
            bars = bars.Where(b => symbols.Contains(b.Symbol)).ToList();
        }

        if (request.From is not null)
        {
            bars = bars.Where(b => b.Timestamp >= request.From.Value).ToList();
        }

        if (request.To is not null)
        {
            bars = bars.Where(b => b.Timestamp <= request.To.Value).ToList();
        }

        if (bars.Count > _options.MaxBars)
        {
            throw new InvalidOperationException($"Replay capped at {_options.MaxBars} bars. Narrow the range.");
        }

        return bars.OrderBy(b => b.Timestamp).ThenBy(b => b.Symbol).ToList();
    }

    private IReadOnlyList<MarketBar> LoadCsv(ReplayRequest request)
    {
        var fileName = string.IsNullOrWhiteSpace(request.CsvFile) ? _options.CsvFile : request.CsvFile.Trim();
        if (fileName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(fileName))
        {
            throw new InvalidOperationException("CSV file must be a file name under the replay root.");
        }

        var root = ResolveRoot();
        var path = Path.GetFullPath(Path.Combine(root, fileName));
        if (!path.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("CSV path must stay under Replay:RootDirectory.");
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Replay CSV not found: {fileName}", path);
        }

        return CsvMarketBarParser.Parse(File.ReadAllText(path));
    }

    private string ResolveRoot()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.RootDirectory)),
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", "samples")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _options.RootDirectory))
        };

        var match = candidates.FirstOrDefault(Directory.Exists);
        return match ?? candidates[0];
    }

    private async Task<IReadOnlyList<MarketBar>> LoadPostgresAsync(ReplayRequest request, CancellationToken cancellationToken)
    {
        var query = _db.MarketBars.AsQueryable();
        if (request.From is not null)
        {
            query = query.Where(b => b.Timestamp >= request.From.Value);
        }

        if (request.To is not null)
        {
            query = query.Where(b => b.Timestamp <= request.To.Value);
        }

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(b => new MarketBar
        {
            Symbol = b.Symbol,
            Timestamp = b.Timestamp,
            Open = b.Open,
            High = b.High,
            Low = b.Low,
            Close = b.Close,
            Volume = b.Volume,
            Source = b.Source
        }).ToList();
    }
}
