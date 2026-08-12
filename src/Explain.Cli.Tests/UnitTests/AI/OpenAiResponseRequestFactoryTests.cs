using Explain.Cli.AI;
using OpenAI.Responses;

namespace Explain.Cli.Tests.UnitTests.AI;

#pragma warning disable OPENAI001
[TestClass]
public class OpenAiResponseRequestFactoryTests
{
    private static readonly ResponseItem[] InputItems =
    [
        ResponseItem.CreateSystemMessageItem("System"),
        ResponseItem.CreateUserMessageItem("Question")
    ];

    [TestMethod]
    public void Create_UsesSelectedModelAndReasoningAndWebSearch()
    {
        var request = new OpenAiRequestSettings("selected-model", ResponseReasoningEffortLevel.Medium);

        var result = OpenAiResponseRequestFactory.Create(InputItems, request);

        Assert.AreEqual("selected-model", result.Model);
        Assert.AreEqual(ResponseReasoningEffortLevel.Medium, result.ReasoningOptions?.ReasoningEffortLevel);
        Assert.AreEqual(false, result.StoredOutputEnabled);
        Assert.HasCount(1, result.Tools);
        Assert.HasCount(2, result.InputItems);
        Assert.IsNull(result.TextOptions);
    }

    [TestMethod]
    public void Create_WithNoReasoning_OmitsReasoningOptions()
    {
        var request = new OpenAiRequestSettings("non-reasoning-model", null);

        var result = OpenAiResponseRequestFactory.Create(InputItems, request);

        Assert.AreEqual("non-reasoning-model", result.Model);
        Assert.IsNull(result.ReasoningOptions);
        Assert.HasCount(1, result.Tools);
    }

    [TestMethod]
    public void Create_WithSchema_ConfiguresStructuredOutput()
    {
        var schema = BinaryData.FromString("""
            {
              "type": "object",
              "properties": { "answer": { "type": "string" } },
              "required": ["answer"],
              "additionalProperties": false
            }
            """);

        var result = OpenAiResponseRequestFactory.Create(
            InputItems,
            new OpenAiRequestSettings("gpt-5.6-terra", ResponseReasoningEffortLevel.Medium),
            "answer_response",
            schema);

        Assert.IsNotNull(result.TextOptions);
        Assert.IsNotNull(result.TextOptions.TextFormat);
    }

    [TestMethod]
    public void Create_WithLocalTools_DisablesParallelCallsAndKeepsWebSearch()
    {
        var localTool = ResponseTool.CreateFunctionTool(
            functionName: "test_tool",
            functionParameters: BinaryData.FromString("""
                {"type":"object","properties":{},"required":[],"additionalProperties":false}
                """),
            strictModeEnabled: true,
            functionDescription: "Test tool");

        var result = OpenAiResponseRequestFactory.Create(
            InputItems,
            new OpenAiRequestSettings("gpt-5.6-terra", ResponseReasoningEffortLevel.Medium),
            localTools: [localTool]);

        Assert.AreEqual(false, result.ParallelToolCallsEnabled);
        Assert.HasCount(2, result.Tools);
    }
}
#pragma warning restore OPENAI001
