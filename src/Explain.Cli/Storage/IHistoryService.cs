namespace Explain.Cli.Storage;

public interface IHistoryService
{
    Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistoryEntry>> GetLatestAsync(int limit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(
        DateTimeOffset cutoffUtc,
        int limit,
        CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
