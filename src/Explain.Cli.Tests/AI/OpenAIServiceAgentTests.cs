using Microsoft.Extensions.Options;
using Explain.Cli.AI;
using Explain.Cli.Configuration;

namespace Explain.Cli.Tests.AI;

[TestClass]
public class OpenAIServiceAgentTests
{
    [TestMethod]
    public void Constructor_MissingApiKey_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new OpenAiOptions { ApiKey = "" });
        
        // Act & Assert
        var exception = Assert.ThrowsException<InvalidOperationException>(
            () => new OpenAIServiceAgent(options));
        
        Assert.IsTrue(exception.Message.Contains("OpenAI API key is missing"));
    }
}
