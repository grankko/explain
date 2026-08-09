using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Explain.Cli.Storage;

public sealed class HistoryService : IHistoryService
{
    private readonly ExplainDatabase _database;

    public HistoryService(ExplainDatabase database)
    {
        _database = database;
    }

    public async Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(entry.OutputText))
            throw new ArgumentException("Output text cannot be empty.", nameof(entry));
        if (string.IsNullOrWhiteSpace(entry.ModelName))
            throw new ArgumentException("Model name cannot be empty.", nameof(entry));
        if (entry.PipedCharacterCount < 0 || entry.PromptTokens < 0 || entry.CompletionTokens < 0 || entry.TotalTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(entry), "Counts cannot be negative.");

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO History (
                RecordedAtUtc, QuestionText, HadPipedInput, PipedCharacterCount,
                OutputText, ModelName, PromptTokens, CompletionTokens, TotalTokens)
            VALUES (
                $recordedAtUtc, $questionText, $hadPipedInput, $pipedCharacterCount,
                $outputText, $modelName, $promptTokens, $completionTokens, $totalTokens);
            """;
        command.Parameters.AddWithValue("$recordedAtUtc", entry.RecordedAtUtc.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$questionText", entry.QuestionText);
        command.Parameters.AddWithValue("$hadPipedInput", entry.HadPipedInput ? 1 : 0);
        command.Parameters.AddWithValue("$pipedCharacterCount", entry.PipedCharacterCount);
        command.Parameters.AddWithValue("$outputText", entry.OutputText);
        command.Parameters.AddWithValue("$modelName", entry.ModelName);
        command.Parameters.AddWithValue("$promptTokens", entry.PromptTokens);
        command.Parameters.AddWithValue("$completionTokens", entry.CompletionTokens);
        command.Parameters.AddWithValue("$totalTokens", entry.TotalTokens);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task<IReadOnlyList<HistoryEntry>> GetLatestAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit < 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "History limit cannot be negative.");

        return ReadAsync(null, limit, cancellationToken);
    }

    public Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(
        DateTimeOffset cutoffUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "Recent history limit must be positive.");

        return ReadAsync(cutoffUtc.ToUniversalTime(), limit, cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA secure_delete=ON; DELETE FROM History;";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var vacuum = connection.CreateCommand();
        vacuum.CommandText = "VACUUM;";
        await vacuum.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<HistoryEntry>> ReadAsync(
        DateTimeOffset? cutoffUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        var history = new List<HistoryEntry>();
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $$"""
            SELECT Id, RecordedAtUtc, QuestionText, HadPipedInput, PipedCharacterCount,
                   OutputText, ModelName, PromptTokens, CompletionTokens, TotalTokens
            FROM History
            {{(cutoffUtc is null ? string.Empty : "WHERE RecordedAtUtc >= $cutoffUtc")}}
            ORDER BY RecordedAtUtc DESC
            {{(limit == 0 ? string.Empty : "LIMIT $limit")}};
            """;
        if (cutoffUtc is not null)
            command.Parameters.AddWithValue("$cutoffUtc", cutoffUtc.Value.ToString("O"));
        if (limit != 0)
            command.Parameters.AddWithValue("$limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            history.Add(new HistoryEntry
            {
                Id = reader.GetInt64(0),
                RecordedAtUtc = DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                QuestionText = reader.GetString(2),
                HadPipedInput = reader.GetBoolean(3),
                PipedCharacterCount = reader.GetInt32(4),
                OutputText = reader.GetString(5),
                ModelName = reader.GetString(6),
                PromptTokens = reader.GetInt32(7),
                CompletionTokens = reader.GetInt32(8),
                TotalTokens = reader.GetInt32(9)
            });
        }

        return history;
    }
}
