using Explain.Cli.AI.Context;
using Explain.Cli.Commands.Explain;
using Explain.Cli.Memory;
using Explain.Cli.Storage;
using OpenAI.Responses;

namespace Explain.Cli.Tests.UnitTests.AI;

#pragma warning disable OPENAI001
[TestClass]
public class AiContextBuilderTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-09T12:00:00Z");

    [TestMethod]
    public async Task Build_AutomaticHistoryUsesRecentTurnsInChronologicalRoles()
    {
        var history = new FakeHistoryService
        {
            Recent =
            [
                Entry(Now.AddMinutes(-1), "newest", "newest answer"),
                Entry(Now.AddMinutes(-2), "older", "older answer")
            ]
        };
        var builder = CreateBuilder(history, new FakeMemoryService());

        var result = await builder.BuildAsync(
            new ExplainInputContent { ArgumentContent = "current" },
            new ExplainArguments());
        var messages = result.Items.OfType<MessageResponseItem>().ToList();

        Assert.AreEqual(MessageRole.System, messages[0].Role);
        Assert.AreEqual(MessageRole.Developer, messages[1].Role);
        Assert.AreEqual(MessageRole.User, messages[2].Role);
        StringAssert.Contains(messages[2].Content[0].Text, "older");
        Assert.AreEqual(MessageRole.Assistant, messages[3].Role);
        Assert.AreEqual("older answer", messages[3].Content[0].Text);
        StringAssert.Contains(messages[4].Content[0].Text, "newest");
        StringAssert.Contains(messages[^1].Content[0].Text, "current");
        Assert.AreEqual(Now.AddHours(-6), history.RequestedCutoff);
        Assert.AreEqual(4, history.RequestedLimit);
    }

    [TestMethod]
    public async Task Build_ExplicitHistoryOverridesWindowAndZeroMeansAll()
    {
        var history = new FakeHistoryService { Latest = [Entry(Now.AddYears(-1), "old", "answer")] };
        var builder = CreateBuilder(history, new FakeMemoryService());

        var result = await builder.BuildAsync(
            new ExplainInputContent { ArgumentContent = "current" },
            new ExplainArguments { IncludeHistory = true, IncludeHistoryLimit = 0 });

        Assert.AreEqual(0, history.RequestedLimit);
        Assert.IsTrue(result.Items.OfType<MessageResponseItem>()
            .Any(message => message.Content.Any(content => content.Text?.Contains("old") == true)));
    }

    [TestMethod]
    public async Task Build_NoHistorySkipsStoredHistoryAndMemory()
    {
        var history = new FakeHistoryService { Recent = [Entry(Now, "secret history", "answer")] };
        var memory = new FakeMemoryService { Records = [Memory("user.fact", "secret memory")] };
        var builder = CreateBuilder(history, memory);

        var result = await builder.BuildAsync(
            new ExplainInputContent { ArgumentContent = "current" },
            new ExplainArguments { NoHistory = true });
        var allText = string.Join("\n", result.Items.OfType<MessageResponseItem>()
            .SelectMany(item => item.Content)
            .Select(content => content.Text));

        Assert.AreEqual(0, history.ReadCalls);
        Assert.AreEqual(0, memory.ReadCalls);
        Assert.IsFalse(allText.Contains("secret history", StringComparison.Ordinal));
        Assert.IsFalse(allText.Contains("secret memory", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Build_HistoryUsesPipedMetadataButNeverRawPipedContent()
    {
        var history = new FakeHistoryService
        {
            Recent =
            [
                new HistoryEntry
                {
                    RecordedAtUtc = Now,
                    QuestionText = "summarize it",
                    HadPipedInput = true,
                    PipedCharacterCount = 999,
                    OutputText = "summary",
                    ModelName = "gpt-5.6-terra"
                }
            ]
        };
        var builder = CreateBuilder(history, new FakeMemoryService());

        var result = await builder.BuildAsync(
            new ExplainInputContent { ArgumentContent = "follow up" },
            new ExplainArguments());
        var allText = string.Join("\n", result.Items.OfType<MessageResponseItem>()
            .SelectMany(item => item.Content)
            .Select(content => content.Text));

        StringAssert.Contains(allText, "999 characters (content was not retained)");
    }

    [TestMethod]
    public async Task Build_IncludesScopedMemoryAndTruncatesLongPriorAnswer()
    {
        var longAnswer = new string('a', 4_000) + "TAIL";
        var history = new FakeHistoryService { Recent = [Entry(Now, "question", longAnswer)] };
        var memory = new FakeMemoryService { Records = [Memory("user.style", "concise")] };
        var builder = CreateBuilder(history, memory);

        var result = await builder.BuildAsync(
            new ExplainInputContent { ArgumentContent = "current" },
            new ExplainArguments());
        var messages = result.Items.OfType<MessageResponseItem>().ToList();
        var context = messages.Single(message => message.Role == MessageRole.Developer).Content[0].Text;
        var priorAnswer = messages.Single(message => message.Role == MessageRole.Assistant).Content[0].Text;

        StringAssert.Contains(context, "user.style: concise");
        StringAssert.Contains(priorAnswer, "...[truncated]...");
        StringAssert.EndsWith(priorAnswer, "TAIL");
        Assert.IsLessThanOrEqualTo(3_000, priorAnswer.Length);
    }

    [TestMethod]
    public async Task Build_StorageFailuresBecomeWarningsAndCurrentRequestContinues()
    {
        var history = new FakeHistoryService { ThrowOnRead = true };
        var memory = new FakeMemoryService { ThrowOnRead = true };
        var builder = CreateBuilder(history, memory);

        var result = await builder.BuildAsync(
            new ExplainInputContent { ArgumentContent = "current" },
            new ExplainArguments());

        Assert.HasCount(2, result.Warnings);
        StringAssert.Contains(
            result.Items.OfType<MessageResponseItem>().Last().Content[0].Text,
            "current");
    }

    private static AiContextBuilder CreateBuilder(IHistoryService history, IMemoryService memory)
    {
        return new AiContextBuilder(history, memory, new ProjectScopeResolver(), new FixedTimeProvider(Now));
    }

    private static HistoryEntry Entry(DateTimeOffset recordedAt, string question, string answer) => new()
    {
        RecordedAtUtc = recordedAt,
        QuestionText = question,
        OutputText = answer,
        ModelName = "gpt-5.6-terra"
    };

    private static MemoryRecord Memory(string key, string value) => new(
        1,
        MemoryScope.Global,
        "",
        key,
        value,
        "other",
        "model",
        Now,
        Now,
        Now);

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeHistoryService : IHistoryService
    {
        public IReadOnlyList<HistoryEntry> Recent { get; set; } = [];
        public IReadOnlyList<HistoryEntry> Latest { get; set; } = [];
        public int ReadCalls { get; private set; }
        public int RequestedLimit { get; private set; }
        public DateTimeOffset? RequestedCutoff { get; private set; }
        public bool ThrowOnRead { get; set; }
        public Task<IReadOnlyList<HistoryEntry>> GetLatestAsync(int limit, CancellationToken cancellationToken = default)
        {
            if (ThrowOnRead)
                throw new IOException("History failed.");
            ReadCalls++;
            RequestedLimit = limit;
            return Task.FromResult(Latest);
        }
        public Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(DateTimeOffset cutoffUtc, int limit, CancellationToken cancellationToken = default)
        {
            if (ThrowOnRead)
                throw new IOException("History failed.");
            ReadCalls++;
            RequestedCutoff = cutoffUtc;
            RequestedLimit = limit;
            return Task.FromResult(Recent);
        }
        public Task AddAsync(HistoryEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMemoryService : IMemoryService
    {
        public IReadOnlyList<MemoryRecord> Records { get; set; } = [];
        public int ReadCalls { get; private set; }
        public bool ThrowOnRead { get; set; }
        public Task<IReadOnlyList<MemoryRecord>> GetForContextAsync(string projectScopeKey, CancellationToken cancellationToken = default)
        {
            if (ThrowOnRead)
                throw new IOException("Memory failed.");
            ReadCalls++;
            return Task.FromResult(Records);
        }
        public Task<IReadOnlyList<MemoryRecord>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(Records);
        public Task<bool> HasSystemProfileAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<MemoryChange>> ApplyAsync(IReadOnlyList<MemoryMutation> mutations, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MemoryChange>>([]);
        public Task ReplaceSystemProfileAsync(IReadOnlyList<SystemFact> facts, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ClearAiMemoriesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
#pragma warning restore OPENAI001
