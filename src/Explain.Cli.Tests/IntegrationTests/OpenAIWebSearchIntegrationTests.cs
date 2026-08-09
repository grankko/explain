using Explain.Cli.AI;
using Explain.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace Explain.Cli.Tests.IntegrationTests;

#pragma warning disable OPENAI001
[TestClass]
public class OpenAIWebSearchIntegrationTests
{
    [TestMethod]
    public async Task CurrentInformationRequest_UsesTerraWebSearchAndReturnsSources()
    {
        if (Environment.GetEnvironmentVariable("EXPLAIN_RUN_LIVE_TESTS") != "1")
            Assert.Inconclusive("Set EXPLAIN_RUN_LIVE_TESTS=1 to run paid OpenAI integration tests.");

        var configPath = ConfigurationPathProvider.GetConfigFilePath();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();
        var settings = configuration.GetSection(OpenAiOptions.SectionName).Get<OpenAiOptions>()
            ?? throw new InvalidOperationException("OpenAI configuration is missing.");
        var agent = new OpenAIServiceAgent(Options.Create(settings));
        var messages = new List<ResponseItem>
        {
            ResponseItem.CreateSystemMessageItem(
                "Answer in one sentence. You must use web search and cite the source."),
            ResponseItem.CreateUserMessageItem(
                "What is one current positive technology news headline from today?")
        };

        var result = await agent.GetResponseAsync(messages, false);

        StringAssert.StartsWith(result.ModelName, "gpt-5.6-terra");
        Assert.IsTrue(result.WebSearchUsed);
        Assert.IsNotEmpty(result.Sources);
        Assert.IsTrue(result.Sources.All(source => Uri.IsWellFormedUriString(source.Url, UriKind.Absolute)));
    }
}
#pragma warning restore OPENAI001
