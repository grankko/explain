using Explain.Cli.AI;
using Explain.Cli.Commands;
using Explain.Cli.Configuration;
using Explain.Cli.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Explain.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Build service provider
            var serviceProvider = CreateServiceProvider(args);
            
            // Get the command and execute it
            var command = serviceProvider.GetRequiredService<ICommand>();
            var exitCode = await command.ExecuteAsync(args);
            
            return exitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application terminated unexpectedly: {ex.Message}");
            return 1;
        }
    }
    
    public static ServiceProvider CreateServiceProvider(string[] args)
    {
        // Ensure the config directory exists
        ConfigurationPathProvider.EnsureApplicationDirectoriesExists();
        
        // Build configuration
        var envOverrides = new Dictionary<string, string?>
        {
            ["OpenAi:ApiKey"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_KEY"),
            ["OpenAi:ModelName"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_MODEL_NAME"),
            ["OpenAi:SmartModelName"] = Environment.GetEnvironmentVariable("EXPLAIN_OPENAI_SMART_MODEL_NAME")
        };

        // Remove null values so they don't override existing configuration
        var sanitizedEnvOverrides = envOverrides
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value!));

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(System.AppContext.BaseDirectory);

        // Look for appsettings.json first in ~/.config/explain/, then in the application directory
        var userConfigPath = ConfigurationPathProvider.GetConfigFilePath();
        if (File.Exists(userConfigPath))
        {
            configBuilder.AddJsonFile(userConfigPath, optional: false, reloadOnChange: false);
        }
        else
        {
            configBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        }
            
        var configuration = configBuilder
            .AddEnvironmentVariables()
            .AddInMemoryCollection(sanitizedEnvOverrides)
            .AddCommandLine(args)
            .Build();

        // Build service collection
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Configure storage options with proper path expansion
        services.Configure<StorageOptions>(storageOptions =>
        {
            var storageConfig = configuration.GetSection(StorageOptions.SectionName);
            storageConfig.Bind(storageOptions);
            
            // If connection string contains placeholder or is empty, use default path
            if (string.IsNullOrEmpty(storageOptions.ConnectionString))
                storageOptions.ConnectionString = $"Data Source={ConfigurationPathProvider.GetDefaultDatabasePath()}";
        });
        
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.AddScoped<IOpenAIServiceAgent, OpenAIServiceAgent>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IConfigurationDisplayService, ConfigurationDisplayService>();
        services.AddScoped<ICommand, ExplainCommand>();

        return services.BuildServiceProvider();
    }
}
