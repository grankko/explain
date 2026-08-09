using Explain.Cli.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Explain.Cli.Storage;

public sealed class ExplainDatabase
{
    private const int SchemaVersion = 2;
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public ExplainDatabase(IOptions<StorageOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(options));
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await ConfigureConnectionAsync(connection, cancellationToken);
        return connection;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await ConfigureConnectionAsync(connection, cancellationToken);

            await using var versionCommand = connection.CreateCommand();
            versionCommand.CommandText = "PRAGMA user_version;";
            var currentVersion = Convert.ToInt32(await versionCommand.ExecuteScalarAsync(cancellationToken));

            if (currentVersion != SchemaVersion)
            {
                await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
                await using var schemaCommand = connection.CreateCommand();
                schemaCommand.Transaction = (SqliteTransaction)transaction;
                schemaCommand.CommandText = $$"""
                    DROP TABLE IF EXISTS History;
                    DROP TABLE IF EXISTS Memories;

                    CREATE TABLE History (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        RecordedAtUtc TEXT NOT NULL,
                        QuestionText TEXT NOT NULL,
                        HadPipedInput INTEGER NOT NULL CHECK (HadPipedInput IN (0, 1)),
                        PipedCharacterCount INTEGER NOT NULL CHECK (PipedCharacterCount >= 0),
                        OutputText TEXT NOT NULL,
                        ModelName TEXT NOT NULL,
                        PromptTokens INTEGER NOT NULL CHECK (PromptTokens >= 0),
                        CompletionTokens INTEGER NOT NULL CHECK (CompletionTokens >= 0),
                        TotalTokens INTEGER NOT NULL CHECK (TotalTokens >= 0)
                    );
                    CREATE INDEX IX_History_RecordedAtUtc ON History (RecordedAtUtc DESC);

                    CREATE TABLE Memories (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Scope TEXT NOT NULL CHECK (Scope IN ('system', 'global', 'project')),
                        ScopeKey TEXT NOT NULL,
                        MemoryKey TEXT NOT NULL,
                        Value TEXT NOT NULL,
                        Category TEXT NOT NULL,
                        Origin TEXT NOT NULL CHECK (Origin IN ('system', 'model')),
                        CreatedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL,
                        VerifiedAtUtc TEXT NOT NULL,
                        UNIQUE (Scope, ScopeKey, MemoryKey)
                    );
                    CREATE INDEX IX_Memories_Scope ON Memories (Scope, ScopeKey, MemoryKey);

                    PRAGMA user_version = {{SchemaVersion}};
                    """;
                await schemaCommand.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            _initialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static async Task ConfigureConnectionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout=5000; PRAGMA journal_mode=WAL; PRAGMA secure_delete=ON;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
