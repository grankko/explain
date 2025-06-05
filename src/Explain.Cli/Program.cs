using Explain.Cli.AI;
using Explain.Cli.Commands;
using Explain.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Explain.Cli;

class Program
{
    static async Task<int> Main(string[] args)
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
    
    static ServiceProvider CreateServiceProvider(string[] args)
    {
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
            .ToDictionary(kv => kv.Key, kv => kv.Value!);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
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
        
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.AddScoped<IOpenAIServiceAgent, OpenAIServiceAgent>();
        services.AddScoped<IConfigurationDisplayService, ConfigurationDisplayService>();
        services.AddScoped<ICommand, ExplainCommand>();

        return services.BuildServiceProvider();
    }
}
