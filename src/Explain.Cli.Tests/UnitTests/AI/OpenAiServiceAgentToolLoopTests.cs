using Explain.Cli.AI;
using Explain.Cli.AI.Tools;
using Explain.Cli.Configuration;
using Explain.Cli.Memory;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace Explain.Cli.Tests.UnitTests.AI;

#pragma warning disable OPENAI001
[TestClass]
public class OpenAiServiceAgentToolLoopTests
{
    [TestMethod]
    public async Task ToolLoop_ReturnsFunctionOutputAndAggregatesMetadata()
    {
        var call = new FunctionCallResponseItem(
            "call-1",
            "test_tool",
            BinaryData.FromString("{}"));
        var api = new FakeResponsesApiClient([
            Result(null, 1, 2, 3, true, [call]),
            Result(
                "Final answer.",
                4,
                5,
                9,
                false,
                [ResponseItem.CreateAssistantMessageItem("Final answer.")],
                [new AiCitation("Source", "https://example.com", 0, 12)])
        ]);
        var toolSession = new FakeToolSession();
        var agent = new OpenAIServiceAgent(Options.Create(Settings()), api);

        var response = await agent.GetResponseAsync(
            [ResponseItem.CreateUserMessageItem("Question")],
            false,
            toolSession);

        Assert.AreEqual(5, response.PromptTokens);
        Assert.AreEqual(7, response.CompletionTokens);
        Assert.AreEqual(12, response.TotalTokens);
        Assert.IsTrue(response.WebSearchUsed);
        Assert.HasCount(1, response.Sources);
        StringAssert.Contains(response.Response, "Final answer[1].");
        Assert.AreEqual(1, toolSession.Executions);
        Assert.HasCount(2, api.Options);
        Assert.IsTrue(api.Options[1].InputItems.Any(item => item is FunctionCallResponseItem));
        Assert.IsTrue(api.Options[1].InputItems.Any(item => item is FunctionCallOutputResponseItem));
        Assert.IsTrue(api.Options.All(options => options.StoredOutputEnabled == false));
    }

    [TestMethod]
    public async Task ToolLoop_RejectsMoreThanFourLocalCalls()
    {
        var results = Enumerable.Range(0, 5)
            .Select(index => Result(
                null,
                1,
                1,
                2,
                false,
                [new FunctionCallResponseItem($"call-{index}", "test_tool", BinaryData.FromString("{}"))]))
            .ToArray();
        var agent = new OpenAIServiceAgent(Options.Create(Settings()), new FakeResponsesApiClient(results));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => agent.GetResponseAsync(
            [ResponseItem.CreateUserMessageItem("Question")],
            false,
            new FakeToolSession()));

        StringAssert.Contains(exception.Message, "limit of 4");
    }

    private static ResponsesApiResult Result(
        string? text,
        int inputTokens,
        int outputTokens,
        int totalTokens,
        bool webSearchUsed,
        IReadOnlyList<ResponseItem> items,
        IReadOnlyList<AiCitation>? citations = null)
    {
        return new ResponsesApiResult(
            text,
            "gpt-5.6-terra",
            inputTokens,
            outputTokens,
            totalTokens,
            webSearchUsed,
            items,
            citations ?? []);
    }

    private static OpenAiOptions Settings() => new()
    {
        ApiKey = "test-key",
        ModelName = "gpt-5.6-terra",
        SmartModelName = "gpt-5.6-terra"
    };

    private sealed class FakeResponsesApiClient : IResponsesApiClient
    {
        private readonly Queue<ResponsesApiResult> _results;
        public FakeResponsesApiClient(IEnumerable<ResponsesApiResult> results) => _results = new Queue<ResponsesApiResult>(results);
        public List<CreateResponseOptions> Options { get; } = [];
        public Task<ResponsesApiResult> CreateAsync(CreateResponseOptions options, CancellationToken cancellationToken = default)
        {
            Options.Add(options);
            return Task.FromResult(_results.Dequeue());
        }
    }

    private sealed class FakeToolSession : ILocalToolSession
    {
        public IReadOnlyList<ResponseTool> Tools { get; } = [];
        public int Executions { get; private set; }
        public Task<string> ExecuteAsync(FunctionCallResponseItem functionCall, CancellationToken cancellationToken = default)
        {
            Executions++;
            return Task.FromResult("{\"status\":\"ok\"}");
        }
        public Task<IReadOnlyList<MemoryChange>> CommitAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MemoryChange>>([]);
    }
}
#pragma warning restore OPENAI001
