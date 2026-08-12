using Microsoft.Extensions.Options;
using Explain.Cli.AI;
using Explain.Cli.Configuration;

namespace Explain.Cli.Tests.UnitTests.AI;

[TestClass]
public class OpenAIServiceAgentTests
{
    [TestMethod]
    public void Constructor_MissingApiKey_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new OpenAiOptions { ApiKey = "" });
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => new OpenAIServiceAgent(options));
        
        Assert.IsTrue(exception.Message.Contains("OpenAI API key is missing"));
    }

    [TestMethod]
    public void OpenAiOptions_DefaultToGpt56TerraWithMediumReasoning()
    {
        var options = new OpenAiOptions();

        Assert.AreEqual("gpt-5.6-terra", options.DefaultModelName);
        Assert.AreEqual("medium", options.DefaultReasoningEffort);
    }
}
