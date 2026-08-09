namespace Explain.Cli.Memory;

public sealed record MemoryRecord(
    long Id,
    MemoryScope Scope,
    string ScopeKey,
    string Key,
    string Value,
    string Category,
    string Origin,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset VerifiedAtUtc);
