using System.Globalization;
using Explain.Cli.Storage;
using Microsoft.Data.Sqlite;

namespace Explain.Cli.Memory;

public sealed class MemoryService : IMemoryService
{
    private const int MaxMemoriesPerScope = 20;
    private const int MaxValueLength = 300;
    private readonly ExplainDatabase _database;
    private readonly TimeProvider _timeProvider;

    public MemoryService(ExplainDatabase database, TimeProvider timeProvider)
    {
        _database = database;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<MemoryRecord>> GetForContextAsync(
        string projectScopeKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Scope, ScopeKey, MemoryKey, Value, Category, Origin,
                   CreatedAtUtc, UpdatedAtUtc, VerifiedAtUtc
            FROM Memories
            WHERE Scope IN ('system', 'global')
               OR (Scope = 'project' AND ScopeKey = $projectScopeKey)
            ORDER BY CASE Scope WHEN 'system' THEN 0 WHEN 'global' THEN 1 ELSE 2 END,
                     MemoryKey;
            """;
        command.Parameters.AddWithValue("$projectScopeKey", projectScopeKey);
        return await ReadAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Scope, ScopeKey, MemoryKey, Value, Category, Origin,
                   CreatedAtUtc, UpdatedAtUtc, VerifiedAtUtc
            FROM Memories
            ORDER BY CASE Scope WHEN 'system' THEN 0 WHEN 'global' THEN 1 ELSE 2 END,
                     ScopeKey, MemoryKey;
            """;
        return await ReadAsync(command, cancellationToken);
    }

    public async Task<bool> HasSystemProfileAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM Memories WHERE Scope = 'system');";
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<IReadOnlyList<MemoryChange>> ApplyAsync(
        IReadOnlyList<MemoryMutation> mutations,
        CancellationToken cancellationToken = default)
    {
        if (mutations.Count == 0)
            return [];

        var changes = new List<MemoryChange>();
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var mutation in mutations)
        {
            switch (mutation.Kind)
            {
                case MemoryMutationKind.Upsert:
                    ValidateAiMutation(mutation);
                    var existed = await ExistsAsync(connection, (SqliteTransaction)transaction, mutation, cancellationToken);
                    if (!existed)
                        await EnsureCapacityAsync(connection, (SqliteTransaction)transaction, mutation, cancellationToken);
                    await UpsertAsync(connection, (SqliteTransaction)transaction, mutation, "model", cancellationToken);
                    changes.Add(new MemoryChange(existed ? "updated" : "added", mutation.Scope, mutation.Key));
                    break;

                case MemoryMutationKind.Delete:
                    ValidateAiMutation(mutation, requireValue: false);
                    if (await DeleteAsync(connection, (SqliteTransaction)transaction, mutation, cancellationToken))
                        changes.Add(new MemoryChange("deleted", mutation.Scope, mutation.Key));
                    break;

                case MemoryMutationKind.RefreshSystem:
                    await ReplaceSystemProfileAsync(
                        connection,
                        (SqliteTransaction)transaction,
                        mutation.SystemFacts ?? [],
                        cancellationToken);
                    changes.Add(new MemoryChange("refreshed", MemoryScope.System, "system.profile"));
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported memory mutation: {mutation.Kind}.");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return changes;
    }

    public async Task ReplaceSystemProfileAsync(
        IReadOnlyList<SystemFact> facts,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ReplaceSystemProfileAsync(connection, (SqliteTransaction)transaction, facts, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ClearAiMemoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA secure_delete=ON; DELETE FROM Memories WHERE Origin = 'model';";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ReplaceSystemProfileAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<SystemFact> facts,
        CancellationToken cancellationToken)
    {
        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM Memories WHERE Scope = 'system';";
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var fact in facts.Where(fact => !string.IsNullOrWhiteSpace(fact.Value)))
        {
            var mutation = new MemoryMutation(
                MemoryMutationKind.Upsert,
                MemoryScope.System,
                string.Empty,
                fact.Key,
                fact.Value.Trim(),
                "system");
            await UpsertAsync(connection, transaction, mutation, "system", cancellationToken);
        }
    }

    private async Task UpsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        MemoryMutation mutation,
        string origin,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().ToString("O");
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO Memories (
                Scope, ScopeKey, MemoryKey, Value, Category, Origin,
                CreatedAtUtc, UpdatedAtUtc, VerifiedAtUtc)
            VALUES ($scope, $scopeKey, $memoryKey, $value, $category, $origin, $now, $now, $now)
            ON CONFLICT (Scope, ScopeKey, MemoryKey) DO UPDATE SET
                Value = excluded.Value,
                Category = excluded.Category,
                Origin = excluded.Origin,
                UpdatedAtUtc = excluded.UpdatedAtUtc,
                VerifiedAtUtc = excluded.VerifiedAtUtc;
            """;
        command.Parameters.AddWithValue("$scope", ToDatabaseValue(mutation.Scope));
        command.Parameters.AddWithValue("$scopeKey", mutation.ScopeKey);
        command.Parameters.AddWithValue("$memoryKey", mutation.Key);
        command.Parameters.AddWithValue("$value", mutation.Value);
        command.Parameters.AddWithValue("$category", mutation.Category);
        command.Parameters.AddWithValue("$origin", origin);
        command.Parameters.AddWithValue("$now", now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> ExistsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        MemoryMutation mutation,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT EXISTS(
                SELECT 1 FROM Memories
                WHERE Scope = $scope AND ScopeKey = $scopeKey AND MemoryKey = $memoryKey);
            """;
        command.Parameters.AddWithValue("$scope", ToDatabaseValue(mutation.Scope));
        command.Parameters.AddWithValue("$scopeKey", mutation.ScopeKey);
        command.Parameters.AddWithValue("$memoryKey", mutation.Key);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    private static async Task EnsureCapacityAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        MemoryMutation mutation,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM Memories WHERE Scope = $scope AND ScopeKey = $scopeKey;";
        command.Parameters.AddWithValue("$scope", ToDatabaseValue(mutation.Scope));
        command.Parameters.AddWithValue("$scopeKey", mutation.ScopeKey);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (count >= MaxMemoriesPerScope)
            throw new InvalidOperationException($"The {mutation.Scope.ToString().ToLowerInvariant()} memory limit of {MaxMemoriesPerScope} has been reached.");
    }

    private static async Task<bool> DeleteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        MemoryMutation mutation,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DELETE FROM Memories
            WHERE Scope = $scope AND ScopeKey = $scopeKey AND MemoryKey = $memoryKey AND Origin = 'model';
            """;
        command.Parameters.AddWithValue("$scope", ToDatabaseValue(mutation.Scope));
        command.Parameters.AddWithValue("$scopeKey", mutation.ScopeKey);
        command.Parameters.AddWithValue("$memoryKey", mutation.Key);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void ValidateAiMutation(MemoryMutation mutation, bool requireValue = true)
    {
        if (mutation.Scope is not (MemoryScope.Global or MemoryScope.Project))
            throw new InvalidOperationException("AI tools cannot modify reserved system memory.");
        if (string.IsNullOrWhiteSpace(mutation.Key) || mutation.Key.Length > 64 ||
            mutation.Key.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')))
            throw new InvalidOperationException("Memory keys must use 1-64 ASCII letters, digits, dots, underscores, or hyphens.");
        if (mutation.Key.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The system.* namespace is reserved.");
        if (requireValue && (string.IsNullOrWhiteSpace(mutation.Value) || mutation.Value.Length > MaxValueLength))
            throw new InvalidOperationException($"Memory values must use 1-{MaxValueLength} characters.");
        if (mutation.Scope == MemoryScope.Project && string.IsNullOrWhiteSpace(mutation.ScopeKey))
            throw new InvalidOperationException("Project memories require a project scope.");
    }

    private static async Task<IReadOnlyList<MemoryRecord>> ReadAsync(
        SqliteCommand command,
        CancellationToken cancellationToken)
    {
        var records = new List<MemoryRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new MemoryRecord(
                reader.GetInt64(0),
                ParseScope(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture)));
        }

        return records;
    }

    private static string ToDatabaseValue(MemoryScope scope) => scope.ToString().ToLowerInvariant();

    private static MemoryScope ParseScope(string value) => value switch
    {
        "system" => MemoryScope.System,
        "global" => MemoryScope.Global,
        "project" => MemoryScope.Project,
        _ => throw new InvalidOperationException($"Unknown memory scope '{value}'.")
    };
}
