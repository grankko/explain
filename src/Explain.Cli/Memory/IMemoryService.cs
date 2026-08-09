namespace Explain.Cli.Memory;

public interface IMemoryService
{
    Task<IReadOnlyList<MemoryRecord>> GetForContextAsync(
        string projectScopeKey,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> HasSystemProfileAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryChange>> ApplyAsync(
        IReadOnlyList<MemoryMutation> mutations,
        CancellationToken cancellationToken = default);
    Task ReplaceSystemProfileAsync(
        IReadOnlyList<SystemFact> facts,
        CancellationToken cancellationToken = default);
    Task ClearAiMemoriesAsync(CancellationToken cancellationToken = default);
}
