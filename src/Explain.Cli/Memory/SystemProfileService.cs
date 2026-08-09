namespace Explain.Cli.Memory;

public sealed class SystemProfileService : ISystemProfileService
{
    private readonly IMemoryService _memoryService;
    private readonly ISystemProfileCollector _collector;

    public SystemProfileService(IMemoryService memoryService, ISystemProfileCollector collector)
    {
        _memoryService = memoryService;
        _collector = collector;
    }

    public async Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (await _memoryService.HasSystemProfileAsync(cancellationToken))
            return false;

        await RefreshAsync(cancellationToken);
        return true;
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        return _memoryService.ReplaceSystemProfileAsync(Collect(), cancellationToken);
    }

    public IReadOnlyList<SystemFact> Collect()
    {
        try
        {
            return _collector.Collect();
        }
        catch
        {
            return [];
        }
    }
}
