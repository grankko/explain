using Explain.Cli.Configuration;
using Explain.Cli.Memory;
using Explain.Cli.Storage;
using Microsoft.Extensions.Options;

namespace Explain.Cli.Tests.UnitTests.Memory;

[TestClass]
public class MemoryServiceTests
{
    private string _databasePath = null!;
    private MemoryService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"test_memory_{Guid.NewGuid()}.sqlite");
        var database = new ExplainDatabase(Options.Create(new StorageOptions
        {
            ConnectionString = $"Data Source={_databasePath}"
        }));
        _service = new MemoryService(database, TimeProvider.System);
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            var path = _databasePath + suffix;
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public async Task Context_ReturnsSystemGlobalAndMatchingProjectOnly()
    {
        await _service.ReplaceSystemProfileAsync([new SystemFact("system.os", "Test OS")]);
        await _service.ApplyAsync([
            Upsert(MemoryScope.Global, "", "user.answer_style", "concise"),
            Upsert(MemoryScope.Project, "/repo/a", "project.framework", ".NET"),
            Upsert(MemoryScope.Project, "/repo/b", "project.framework", "Python")
        ]);

        var memories = await _service.GetForContextAsync("/repo/a");

        Assert.HasCount(3, memories);
        Assert.IsTrue(memories.Any(memory => memory.Key == "system.os"));
        Assert.IsTrue(memories.Any(memory => memory.Value == ".NET"));
        Assert.IsFalse(memories.Any(memory => memory.Value == "Python"));
    }

    [TestMethod]
    public async Task Apply_UpsertsDeletesAndReportsChanges()
    {
        var added = await _service.ApplyAsync([Upsert(MemoryScope.Global, "", "user.locale", "Sweden")]);
        var updated = await _service.ApplyAsync([Upsert(MemoryScope.Global, "", "user.locale", "Stockholm")]);
        var deleted = await _service.ApplyAsync([
            new MemoryMutation(MemoryMutationKind.Delete, MemoryScope.Global, "", "user.locale", "", "")
        ]);

        Assert.AreEqual("added", added.Single().Action);
        Assert.AreEqual("updated", updated.Single().Action);
        Assert.AreEqual("deleted", deleted.Single().Action);
        Assert.IsEmpty((await _service.GetAllAsync()).Where(memory => memory.Origin == "model"));
    }

    [TestMethod]
    public async Task Apply_ProtectsReservedSystemNamespace()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApplyAsync([
            Upsert(MemoryScope.Global, "", "system.os", "Invented")
        ]));
    }

    [TestMethod]
    public async Task Apply_EnforcesTwentyMemoriesPerAiScope()
    {
        var mutations = Enumerable.Range(0, 20)
            .Select(index => Upsert(MemoryScope.Global, "", $"fact.{index}", $"value {index}"))
            .ToArray();
        await _service.ApplyAsync(mutations);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApplyAsync([
            Upsert(MemoryScope.Global, "", "fact.overflow", "too many")
        ]));
        Assert.HasCount(20, (await _service.GetAllAsync()).Where(memory => memory.Origin == "model"));
    }

    [TestMethod]
    public async Task ClearAiMemories_PreservesSystemProfile()
    {
        await _service.ReplaceSystemProfileAsync([new SystemFact("system.os", "Test OS")]);
        await _service.ApplyAsync([Upsert(MemoryScope.Global, "", "user.locale", "Sweden")]);

        await _service.ClearAiMemoriesAsync();

        var memories = await _service.GetAllAsync();
        Assert.HasCount(1, memories);
        Assert.AreEqual(MemoryScope.System, memories[0].Scope);
    }

    private static MemoryMutation Upsert(
        MemoryScope scope,
        string scopeKey,
        string key,
        string value)
    {
        return new MemoryMutation(MemoryMutationKind.Upsert, scope, scopeKey, key, value, "other");
    }
}
