using Explain.Cli.AI;

namespace Explain.Cli.Tests.UnitTests.AI;

[TestClass]
public class OpenAiResponseMapperTests
{
    [TestMethod]
    public void Map_StringResponse_PreservesMetadataAndSources()
    {
        var result = OpenAiResponseMapper.Map<string>(
            "Current fact.",
            "gpt-5.6-terra",
            10,
            20,
            30,
            true,
            [new AiCitation("Source", "https://example.com/fact", 0, 12)],
            false);

        Assert.AreEqual("gpt-5.6-terra", result.ModelName);
        Assert.AreEqual(10, result.PromptTokens);
        Assert.AreEqual(20, result.CompletionTokens);
        Assert.AreEqual(30, result.TotalTokens);
        Assert.IsTrue(result.WebSearchUsed);
        Assert.HasCount(1, result.Sources);
        StringAssert.Contains(result.Response, "Current fact[1].");
    }

    [TestMethod]
    public void Map_StructuredResponse_DeserializesWithoutChangingJson()
    {
        var result = OpenAiResponseMapper.Map<TestResponse>(
            "{\"answer\":\"yes\"}",
            "gpt-5.6-terra",
            1,
            2,
            3,
            false,
            [],
            true);

        Assert.AreEqual("yes", result.Response.Answer);
    }

    [TestMethod]
    public void Map_EmptyResponse_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            OpenAiResponseMapper.Map<string>(" ", "gpt-5.6-terra", 0, 0, 0, false, [], false));

        StringAssert.Contains(exception.Message, "empty response");
    }

    private sealed record TestResponse(string Answer);
}
