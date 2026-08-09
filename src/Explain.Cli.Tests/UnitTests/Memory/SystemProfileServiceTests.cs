using Explain.Cli.Memory;

namespace Explain.Cli.Tests.UnitTests.Memory;

[TestClass]
public class SystemProfileServiceTests
{
    [TestMethod]
    public void Collector_ReturnsPrivacyBalancedFactsOnly()
    {
        var facts = new SystemProfileCollector().Collect();
        var keys = facts.Select(fact => fact.Key).ToArray();

        CollectionAssert.IsSubsetOf(
            keys,
            new[] { "system.os", "system.kernel", "system.architecture", "system.default_shell", "system.timezone" });
        Assert.IsFalse(keys.Any(key => key.Contains("host", StringComparison.OrdinalIgnoreCase)));
        Assert.IsFalse(keys.Any(key => key.Contains("user", StringComparison.OrdinalIgnoreCase)));
        var shell = facts.SingleOrDefault(fact => fact.Key == "system.default_shell")?.Value;
        if (shell is not null)
            Assert.AreEqual(Path.GetFileName(shell), shell);
    }

    [TestMethod]
    public async Task EnsureInitialized_CollectsOnceAndRefreshReplacesProfile()
    {
        var memory = new FakeMemoryService();
        var collector = new FakeCollector([new SystemFact("system.os", "First OS")]);
        var service = new SystemProfileService(memory, collector);

        Assert.IsTrue(await service.EnsureInitializedAsync());
        Assert.IsFalse(await service.EnsureInitializedAsync());
        collector.Facts = [new SystemFact("system.os", "Updated OS")];
        await service.RefreshAsync();

        Assert.AreEqual(2, memory.ReplaceCalls);
        Assert.AreEqual("Updated OS", memory.SystemFacts.Single().Value);
    }

    private sealed class FakeCollector : ISystemProfileCollector
    {
        public FakeCollector(IReadOnlyList<SystemFact> facts) => Facts = facts;
        public IReadOnlyList<SystemFact> Facts { get; set; }
        public IReadOnlyList<SystemFact> Collect() => Facts;
    }

    private sealed class FakeMemoryService : IMemoryService
    {
        public IReadOnlyList<SystemFact> SystemFacts { get; private set; } = [];
        public int ReplaceCalls { get; private set; }
        public Task<bool> HasSystemProfileAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SystemFacts.Count > 0);
        public Task ReplaceSystemProfileAsync(IReadOnlyList<SystemFact> facts, CancellationToken cancellationToken = default)
        {
            SystemFacts = facts;
            ReplaceCalls++;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<MemoryRecord>> GetForContextAsync(string projectScopeKey, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MemoryRecord>>([]);
        public Task<IReadOnlyList<MemoryRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MemoryRecord>>([]);
        public Task<IReadOnlyList<MemoryChange>> ApplyAsync(IReadOnlyList<MemoryMutation> mutations, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MemoryChange>>([]);
        public Task ClearAiMemoriesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
