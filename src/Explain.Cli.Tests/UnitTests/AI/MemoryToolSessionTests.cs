using Explain.Cli.AI.Tools;
using Explain.Cli.Memory;
using OpenAI.Responses;

namespace Explain.Cli.Tests.UnitTests.AI;

#pragma warning disable OPENAI001
[TestClass]
public class MemoryToolSessionTests
{
    [TestMethod]
    public async Task Upsert_IsStagedUntilCommitAndUsesProjectScopeKey()
    {
        var memory = new FakeMemoryService();
        var session = new MemoryToolSession(memory, new FakeSystemProfileService(), "/repo", "");
        var call = new FunctionCallResponseItem(
            "call-1",
            MemoryToolSession.UpsertToolName,
            BinaryData.FromString("""
                {"scope":"project","key":"project.framework","value":"Uses .NET 10","category":"project_fact"}
                """));

        var output = await session.ExecuteAsync(call);

        StringAssert.Contains(output, "queued");
        Assert.IsEmpty(memory.Applied);
        var changes = await session.CommitAsync();
        Assert.HasCount(1, memory.Applied);
        Assert.AreEqual("/repo", memory.Applied[0].ScopeKey);
        Assert.AreEqual("project.framework", changes.Single().Key);
    }

    [TestMethod]
    public async Task Upsert_RejectsRawPipedContentAndPotentialSecrets()
    {
        var memory = new FakeMemoryService();
        var session = new MemoryToolSession(memory, new FakeSystemProfileService(), "/repo", "raw private value from pipe");
        var pipedCall = UpsertCall("raw private value from pipe");
        var secretCall = UpsertCall("sk-example-secret-value");

        StringAssert.Contains(await session.ExecuteAsync(pipedCall), "error");
        StringAssert.Contains(await session.ExecuteAsync(secretCall), "error");
        await session.CommitAsync();

        Assert.IsEmpty(memory.Applied);
    }

    [TestMethod]
    public async Task RefreshSystem_StagesDeterministicFacts()
    {
        var memory = new FakeMemoryService();
        var system = new FakeSystemProfileService
        {
            Facts = [new SystemFact("system.os", "Test OS")]
        };
        var session = new MemoryToolSession(memory, system, "/repo");
        var call = new FunctionCallResponseItem(
            "call-2",
            MemoryToolSession.RefreshSystemToolName,
            BinaryData.FromString("{}"));

        var output = await session.ExecuteAsync(call);
        await session.CommitAsync();

        StringAssert.Contains(output, "1 facts");
        Assert.AreEqual(MemoryMutationKind.RefreshSystem, memory.Applied.Single().Kind);
        Assert.AreEqual("Test OS", memory.Applied.Single().SystemFacts!.Single().Value);
    }

    [TestMethod]
    public void Tools_ExposeThreeStrictFunctionDefinitions()
    {
        var session = new MemoryToolSession(new FakeMemoryService(), new FakeSystemProfileService(), "/repo");

        Assert.HasCount(3, session.Tools);
        Assert.IsTrue(session.Tools.All(tool => tool is FunctionTool));
    }

    private static FunctionCallResponseItem UpsertCall(string value)
    {
        return new FunctionCallResponseItem(
            Guid.NewGuid().ToString(),
            MemoryToolSession.UpsertToolName,
            BinaryData.FromString($$"""
                {"scope":"global","key":"user.fact","value":{{System.Text.Json.JsonSerializer.Serialize(value)}},"category":"user_fact"}
                """));
    }

    private sealed class FakeMemoryService : IMemoryService
    {
        public IReadOnlyList<MemoryMutation> Applied { get; private set; } = [];
        public Task<IReadOnlyList<MemoryChange>> ApplyAsync(IReadOnlyList<MemoryMutation> mutations, CancellationToken cancellationToken = default)
        {
            Applied = mutations.ToArray();
            return Task.FromResult<IReadOnlyList<MemoryChange>>(mutations.Select(mutation =>
                new MemoryChange("added", mutation.Scope, mutation.Key)).ToArray());
        }
        public Task<IReadOnlyList<MemoryRecord>> GetForContextAsync(string projectScopeKey, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MemoryRecord>>([]);
        public Task<IReadOnlyList<MemoryRecord>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MemoryRecord>>([]);
        public Task<bool> HasSystemProfileAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task ReplaceSystemProfileAsync(IReadOnlyList<SystemFact> facts, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearAiMemoriesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeSystemProfileService : ISystemProfileService
    {
        public IReadOnlyList<SystemFact> Facts { get; set; } = [];
        public IReadOnlyList<SystemFact> Collect() => Facts;
        public Task<bool> EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
#pragma warning restore OPENAI001
