using Explain.Cli.Configuration;
using Explain.Cli.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Explain.Cli.Tests.UnitTests.Storage;

[TestClass]
public class HistoryServiceTests
{
    private string _testDatabasePath = null!;
    private ExplainDatabase _database = null!;
    private HistoryService _historyService = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_history_{Guid.NewGuid()}.sqlite");
        _database = CreateDatabase();
        _historyService = new HistoryService(_database);
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            var path = _testDatabasePath + suffix;
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public async Task AddAndRead_PreservesMetadataAndOrdersNewestFirst()
    {
        await _historyService.AddAsync(CreateEntry(DateTimeOffset.Parse("2026-08-09T08:00:00Z"), "first"));
        await _historyService.AddAsync(CreateEntry(DateTimeOffset.Parse("2026-08-09T09:00:00Z"), "second", true, 123));

        var history = await _historyService.GetLatestAsync(5);

        Assert.HasCount(2, history);
        Assert.AreEqual("second", history[0].QuestionText);
        Assert.IsTrue(history[0].HadPipedInput);
        Assert.AreEqual(123, history[0].PipedCharacterCount);
        Assert.AreEqual(TimeSpan.Zero, history[0].RecordedAtUtc.Offset);
    }

    [TestMethod]
    public async Task GetRecent_FiltersByUtcCutoffAndLimit()
    {
        await _historyService.AddAsync(CreateEntry(DateTimeOffset.Parse("2026-08-09T01:00:00Z"), "old"));
        await _historyService.AddAsync(CreateEntry(DateTimeOffset.Parse("2026-08-09T07:00:00Z"), "recent"));
        await _historyService.AddAsync(CreateEntry(DateTimeOffset.Parse("2026-08-09T08:00:00Z"), "newest"));

        var history = await _historyService.GetRecentAsync(DateTimeOffset.Parse("2026-08-09T06:00:00Z"), 1);

        Assert.HasCount(1, history);
        Assert.AreEqual("newest", history[0].QuestionText);
    }

    [TestMethod]
    public async Task GetLatest_ZeroReturnsAllAndNegativeThrows()
    {
        await _historyService.AddAsync(CreateEntry(DateTimeOffset.UtcNow, "one"));

        Assert.HasCount(1, await _historyService.GetLatestAsync(0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _historyService.GetLatestAsync(-1));
    }

    [TestMethod]
    public async Task Clear_WithEmptyOrPopulatedDatabase_DoesNotThrow()
    {
        await _historyService.ClearAsync();
        await _historyService.AddAsync(CreateEntry(DateTimeOffset.UtcNow, "one"));

        await _historyService.ClearAsync();

        Assert.IsEmpty(await _historyService.GetLatestAsync(0));
    }

    [TestMethod]
    public async Task OpeningLegacyDatabase_ReplacesSchemaAndDeletesOldHistory()
    {
        await using (var connection = new SqliteConnection($"Data Source={_testDatabasePath}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE History (Id INTEGER PRIMARY KEY, InputText TEXT); INSERT INTO History VALUES (1, 'legacy');";
            await command.ExecuteNonQueryAsync();
        }

        _database = CreateDatabase();
        _historyService = new HistoryService(_database);

        Assert.IsEmpty(await _historyService.GetLatestAsync(0));
        await using var verified = await _database.OpenConnectionAsync();
        await using var versionCommand = verified.CreateCommand();
        versionCommand.CommandText = "PRAGMA user_version;";
        Assert.AreEqual(2L, (long)(await versionCommand.ExecuteScalarAsync())!);
    }

    [TestMethod]
    public async Task BundledSqliteVersion_IsNotVulnerableVersion()
    {
        await using var connection = await _database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version();";
        var version = Version.Parse((string)(await command.ExecuteScalarAsync())!);

        Assert.IsGreaterThanOrEqualTo(new Version(3, 50, 2), version);
    }

    private ExplainDatabase CreateDatabase()
    {
        return new ExplainDatabase(Options.Create(new StorageOptions
        {
            ConnectionString = $"Data Source={_testDatabasePath}"
        }));
    }

    private static HistoryEntry CreateEntry(
        DateTimeOffset recordedAtUtc,
        string question,
        bool piped = false,
        int pipedCharacters = 0)
    {
        return new HistoryEntry
        {
            RecordedAtUtc = recordedAtUtc,
            QuestionText = question,
            HadPipedInput = piped,
            PipedCharacterCount = pipedCharacters,
            OutputText = "answer",
            ModelName = "gpt-5.6-terra",
            PromptTokens = 1,
            CompletionTokens = 2,
            TotalTokens = 3
        };
    }
}
