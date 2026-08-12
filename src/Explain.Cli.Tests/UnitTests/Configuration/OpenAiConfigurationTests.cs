using Explain.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Explain.Cli.Tests.UnitTests.Configuration;

[TestClass]
public class OpenAiConfigurationTests
{
    [TestMethod]
    public void EnvironmentVariables_Override_AppSettings()
    {
        var appSettings = new Dictionary<string, string?>
        {
            ["OpenAi:ApiKey"] = "json-key",
            ["OpenAi:DefaultModelName"] = "json-model",
            ["OpenAi:DefaultReasoningEffort"] = "low"
        };

        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_KEY", "env-key");
        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_DEFAULT_MODEL_NAME", "env-model");
        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_DEFAULT_REASONING_EFFORT", "high");

        var envOverrides = new Dictionary<string, string?>
        {
            ["OpenAi:ApiKey"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_KEY"),
            ["OpenAi:DefaultModelName"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_DEFAULT_MODEL_NAME"),
            ["OpenAi:DefaultReasoningEffort"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_DEFAULT_REASONING_EFFORT")
        };
        var sanitizedEnvOverrides = envOverrides
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value!));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(appSettings!)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(sanitizedEnvOverrides)
            .Build();

        var services = new ServiceCollection();
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<OpenAiOptions>>().Value;

        Assert.AreEqual("env-key", options.ApiKey);
        Assert.AreEqual("env-model", options.DefaultModelName);
        Assert.AreEqual("high", options.DefaultReasoningEffort);

        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_KEY", null);
        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_DEFAULT_MODEL_NAME", null);
        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_DEFAULT_REASONING_EFFORT", null);
    }
}
