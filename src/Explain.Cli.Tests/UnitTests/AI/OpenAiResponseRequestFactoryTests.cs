using Explain.Cli.AI;
using Explain.Cli.Configuration;
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
    public void Create_RegularMode_UsesRegularModelLowReasoningAndWebSearch()
    {
        var settings = new OpenAiOptions
        {
            ModelName = "regular-model",
            SmartModelName = "smart-model"
        };

        var result = OpenAiResponseRequestFactory.Create(settings, InputItems, false);

        Assert.AreEqual("regular-model", result.Model);
        Assert.AreEqual(ResponseReasoningEffortLevel.Low, result.ReasoningOptions?.ReasoningEffortLevel);
        Assert.AreEqual(false, result.StoredOutputEnabled);
        Assert.HasCount(1, result.Tools);
        Assert.HasCount(2, result.InputItems);
        Assert.IsNull(result.TextOptions);
    }

    [TestMethod]
    public void Create_ThinkMode_UsesSmartModelAndHighReasoning()
    {
        var settings = new OpenAiOptions
        {
            ModelName = "regular-model",
            SmartModelName = "smart-model"
        };

        var result = OpenAiResponseRequestFactory.Create(settings, InputItems, true);

        Assert.AreEqual("smart-model", result.Model);
        Assert.AreEqual(ResponseReasoningEffortLevel.High, result.ReasoningOptions?.ReasoningEffortLevel);
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
            new OpenAiOptions(),
            InputItems,
            false,
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
            new OpenAiOptions(),
            InputItems,
            false,
            localTools: [localTool]);

        Assert.AreEqual(false, result.ParallelToolCallsEnabled);
        Assert.HasCount(2, result.Tools);
    }
}
#pragma warning restore OPENAI001
