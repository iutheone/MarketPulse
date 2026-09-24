using MarketPulse.Application.Contracts;
using MarketPulse.Application.Queries;

namespace MarketPulse.Application.Interfaces;

public interface IBarHistoryStore
{
    Task<(IReadOnlyList<BarDto> Items, int Total)> SearchAsync(HistoryQuery query, CancellationToken cancellationToken);
}

public interface IWatchlistStore
{
    Task<IReadOnlyList<WatchlistDto>> ListAsync(CancellationToken cancellationToken);
    Task<WatchlistDto> CreateAsync(string name, IReadOnlyList<string> symbols, CancellationToken cancellationToken);
}

public interface IDetectionRuleStore
{
    Task<IReadOnlyList<DetectionRuleDto>> ListAsync(CancellationToken cancellationToken);
    Task<DetectionRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DetectionRuleDto> CreateAsync(UpsertDetectionRuleRequest request, CancellationToken cancellationToken);
    Task<DetectionRuleDto?> UpdateAsync(Guid id, UpsertDetectionRuleRequest request, CancellationToken cancellationToken);
}
