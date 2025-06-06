using Microsoft.Extensions.Options;

namespace Explain.Cli.Configuration
{
    public class ConfigurationDisplayService : IConfigurationDisplayService
    {
        private readonly OpenAiOptions _openAiOptions;
        private readonly StorageOptions _storageOptions;

        public ConfigurationDisplayService(IOptions<OpenAiOptions> openAiOptions, IOptions<StorageOptions> storageOptions)
        {
            _openAiOptions = openAiOptions.Value;
            _storageOptions = storageOptions.Value;
        }

        public void DisplayConfiguration()
        {
            Console.WriteLine("=== Configuration Test ===");
            Console.WriteLine($"API Key: {(!string.IsNullOrEmpty(_openAiOptions.ApiKey) ? "***CONFIGURED***" : "NOT SET")}");
            Console.WriteLine($"Model Name: {_openAiOptions.ModelName}");
            Console.WriteLine($"Smart Model Name: {_openAiOptions.SmartModelName}");
            Console.WriteLine($"Organization: {_openAiOptions.Organization ?? "Not set"}");
            Console.WriteLine("=== Storage Configuration ===");
            Console.WriteLine($"Connection String: {(string.IsNullOrEmpty(_storageOptions.ConnectionString) ? "NOT SET" : "***CONFIGURED***")}");
            Console.WriteLine("===========================");
        }
    }
}