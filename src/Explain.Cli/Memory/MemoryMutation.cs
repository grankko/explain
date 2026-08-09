namespace Explain.Cli.Memory;

public enum MemoryMutationKind
{
    Upsert,
    Delete,
    RefreshSystem
}

public sealed record MemoryMutation(
    MemoryMutationKind Kind,
    MemoryScope Scope,
    string ScopeKey,
    string Key,
    string Value,
    string Category,
    IReadOnlyList<SystemFact>? SystemFacts = null);

public sealed record MemoryChange(string Action, MemoryScope Scope, string Key);
