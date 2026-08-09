using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Explain.Cli.AI;
using Explain.Cli.Commands;
using Explain.Cli.Configuration;
using Explain.Cli.AI.Context;
using Explain.Cli.AI.Tools;
using Explain.Cli.Memory;
using Explain.Cli.Storage;

namespace Explain.Cli.Tests.UnitTests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public void AppVersion_IsSemanticVersionFromVersionFile()
    {
        StringAssert.Matches(AppVersion.Current, new System.Text.RegularExpressions.Regex(@"^\d+\.\d+\.\d+$"));
    }

    [TestMethod]
    public void CreateServiceProvider_ConfigurationLoaded_HasValidOpenAiOptions()
    {
        // Arrange
        var args = new[] { "test" };
        
        // Act
        var serviceProvider = Program.CreateServiceProvider(args);
        var openAiOptions = serviceProvider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
        
        // Assert
        Assert.IsNotNull(openAiOptions, "OpenAiOptions should be configured");
        Assert.IsFalse(string.IsNullOrWhiteSpace(openAiOptions.ApiKey), "ApiKey should be configured");
        Assert.IsFalse(string.IsNullOrWhiteSpace(openAiOptions.ModelName), "ModelName should be configured");
        Assert.IsFalse(string.IsNullOrWhiteSpace(openAiOptions.SmartModelName), "SmartModelName should be configured");
    }

    [TestMethod]
    public void CreateServiceProvider_AllServicesRegistered_CanResolveServices()
    {
        // Arrange
        var args = new[] { "test" };
        
        // Act
        var serviceProvider = Program.CreateServiceProvider(args);
        
        // Assert
        Assert.IsNotNull(serviceProvider.GetRequiredService<ICommand>(), "ICommand should be registered");
        Assert.IsNotNull(serviceProvider.GetRequiredService<IOpenAIServiceAgent>(), "IOpenAIServiceAgent should be registered");
        Assert.IsNotNull(serviceProvider.GetRequiredService<IHistoryService>());
        Assert.IsNotNull(serviceProvider.GetRequiredService<IMemoryService>());
        Assert.IsNotNull(serviceProvider.GetRequiredService<ISystemProfileService>());
        Assert.IsNotNull(serviceProvider.GetRequiredService<IAiContextBuilder>());
        Assert.IsNotNull(serviceProvider.GetRequiredService<ILocalToolSessionFactory>());
        Assert.IsNotNull(serviceProvider.GetRequiredService<IConfigurationDisplayService>(), "IConfigurationDisplayService should be registered");
    }

    [TestMethod]
    public void CreateServiceProvider_EnvironmentVariablesOverride_ConfigurationValues()
    {
        // Arrange
        var originalApiKey = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_KEY");
        var testApiKey = "test-env-api-key";
        
        try
        {
            Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_KEY", testApiKey);
            var args = new[] { "test" };
            
            // Act
            var serviceProvider = Program.CreateServiceProvider(args);
            var openAiOptions = serviceProvider.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            
            // Assert
            Assert.AreEqual(testApiKey, openAiOptions.ApiKey, "Environment variable should override configuration");
        }
        finally
        {
            Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_KEY", originalApiKey);
        }
    }
}
