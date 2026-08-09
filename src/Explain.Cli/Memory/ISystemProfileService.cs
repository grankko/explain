namespace Explain.Cli.Memory;

public interface ISystemProfileService
{
    Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<SystemFact> Collect();
}
