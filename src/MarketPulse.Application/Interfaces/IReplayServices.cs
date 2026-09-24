using MarketPulse.Application.Contracts;
using MarketPulse.Domain.Entities;

namespace MarketPulse.Application.Interfaces;

public interface IHistoricalBarLoader
{
    Task<IReadOnlyList<MarketBar>> LoadAsync(ReplayRequest request, CancellationToken cancellationToken);
}

public interface IHistoricalReplayService
{
    Task<ReplayResultDto> ReplayAsync(ReplayRequest request, CancellationToken cancellationToken);
}

public interface IBacktestService
{
    Task<BacktestRunDto> RunAsync(BacktestRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<BacktestRunDto>> ListAsync(CancellationToken cancellationToken);
    Task<BacktestRunDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}

public interface IBacktestStore
{
    Task<BacktestRunDto> SaveAsync(BacktestRunDto run, CancellationToken cancellationToken);
    Task<IReadOnlyList<BacktestRunDto>> ListAsync(CancellationToken cancellationToken);
    Task<BacktestRunDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
