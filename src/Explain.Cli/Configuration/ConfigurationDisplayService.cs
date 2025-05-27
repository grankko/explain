using Explain.Cli.Configuration;
using Microsoft.Extensions.Options;

public class ConfigurationDisplayService : IConfigurationDisplayService
{
    private readonly OpenAiOptions _openAiOptions;

    public ConfigurationDisplayService(IOptions<OpenAiOptions> openAiOptions)
    {
        _openAiOptions = openAiOptions.Value;
    }

    public void DisplayConfiguration()
    {
        Console.WriteLine("=== Configuration Test ===");
        Console.WriteLine($"API Key: {(!string.IsNullOrEmpty(_openAiOptions.ApiKey) ? "***CONFIGURED***" : "NOT SET")}");
        Console.WriteLine($"Model Name: {_openAiOptions.ModelName}");
        Console.WriteLine($"Organization: {_openAiOptions.Organization ?? "Not set"}");
        Console.WriteLine("===========================");
    }
}
