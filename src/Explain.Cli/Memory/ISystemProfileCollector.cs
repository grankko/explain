namespace Explain.Cli.Memory;

public sealed record SystemFact(string Key, string Value);

public interface ISystemProfileCollector
{
    IReadOnlyList<SystemFact> Collect();
}
