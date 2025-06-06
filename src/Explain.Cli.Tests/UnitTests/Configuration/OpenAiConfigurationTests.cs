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
            ["OpenAi:ModelName"] = "json-model",
            ["OpenAi:SmartModelName"] = "json-smart"
        };

        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_KEY", "env-key");
        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_MODEL_NAME", "env-model");
        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_SMART_MODEL_NAME", "env-smart");

        var envOverrides = new Dictionary<string, string?>
        {
            ["OpenAi:ApiKey"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_KEY"),
            ["OpenAi:ModelName"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_MODEL_NAME"),
            ["OpenAi:SmartModelName"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_SMART_MODEL_NAME")
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
        Assert.AreEqual("env-model", options.ModelName);
        Assert.AreEqual("env-smart", options.SmartModelName);

        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_KEY", null);
        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_MODEL_NAME", null);
        Environment.SetEnvironmentVariable("EXPLAIN_OPENAI_SMART_MODEL_NAME", null);
    }
}
