using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Explain.Cli.Configuration;
using System.Text.Json;

namespace Explain.Cli.AI;

/// <summary>
/// Agent for interacting with the OpenAI API.
/// </summary>
public class OpenAIServiceAgent : IOpenAIServiceAgent
{
    private readonly OpenAiOptions _openAiOptions;
    private ChatClient _chatClient;
    private bool _isVerbose = false;

    /// <summary>
    /// Gets or sets whether to show verbose output.
    /// </summary>
    public bool IsVerbose
    {
        get => _isVerbose;
        set => _isVerbose = value;
    }

    public OpenAIServiceAgent(IOptions<OpenAiOptions> openAiOptions)
    {
        _openAiOptions = openAiOptions.Value;

        if (string.IsNullOrWhiteSpace(_openAiOptions.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is missing. Please update your appsettings.json file with a valid API key.");
        }

        // Create the OpenAI ChatClient with the model and API key
        _chatClient = new ChatClient(_openAiOptions.ModelName, _openAiOptions.ApiKey);
    }

    public async Task<string> GetChatCompletionAsync(List<ChatMessage> messages)
    {
        return await GetTypedChatCompletionAsync<string>(messages, null, null);
    }

    public async Task<T> GetTypedChatCompletionAsync<T>(List<ChatMessage> messages, string? schemaName, BinaryData? jsonSchema)
    {
        try
        {
            // Call the OpenAI API with chat completion options
            var options = new ChatCompletionOptions
            {
                Temperature = 0.1f, // Lower temperature for more deterministic responses
#pragma warning disable OPENAI001
                Seed = 1235431345345L,
#pragma warning restore OPENAI001
            };

            bool attemptedToUseStructuredOutput = false;

            // Add structured output support if schema is provided
            if (!string.IsNullOrWhiteSpace(schemaName) && jsonSchema != null)
            {
                attemptedToUseStructuredOutput = true;
                options.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: schemaName,
                    jsonSchema: jsonSchema,
                    jsonSchemaFormatDescription: $"Structured response according to {schemaName} schema",
                    jsonSchemaIsStrict: true
                );

                if (_isVerbose)
                    Console.WriteLine($"Using structured output with schema: {schemaName}");
            }

            // Get the response from the API
            var result = await _chatClient.CompleteChatAsync(messages, options);
            var textResponse = result.Value.Content.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(textResponse))
                throw new InvalidOperationException("Received empty response from OpenAI.");

            
            // If structured output was attempted, deserialize to the specified type, if not, assume string response
            if (attemptedToUseStructuredOutput)
                return JsonSerializer.Deserialize<T>(textResponse)
                    ?? throw new InvalidOperationException("Failed to deserialize OpenAI response.");
            else if (typeof(T) == typeof(string))
                return (T)(object)textResponse!;
            else
                throw new InvalidOperationException($"Cannot convert response to type {typeof(T).Name}.");

        }
        catch (Exception ex)
        {
            // Log the error only in verbose mode
            if (_isVerbose)
                Console.WriteLine($"Error in GetChatCompletion: {ex.Message}");

            throw;
        }
    }
}
