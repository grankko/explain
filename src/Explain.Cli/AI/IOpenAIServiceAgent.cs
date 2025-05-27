using OpenAI.Chat;

namespace Explain.Cli.AI;

/// <summary>
/// Interface for interacting with the OpenAI API.
/// </summary>
public interface IOpenAIServiceAgent
{
    /// <summary>
    /// Gets or sets whether to show verbose output.
    /// </summary>
    bool IsVerbose { get; set; }

    /// <summary>
    /// Gets a chat completion response from OpenAI.
    /// </summary>
    /// <param name="messages">The chat messages to send</param>
    /// <param name="thinkDeep">Whether to use a more complex model for deeper thinking</param>
    /// <returns>The completion response</returns>
    Task<string> GetChatCompletionAsync(List<ChatMessage> messages, bool thinkDeep);

    /// <summary>
    /// Gets a chat completion response from OpenAI with optional structured output.
    /// </summary>
    /// <param name="messages">The chat messages to send</param>
    /// <param name="thinkDeep">Whether to use a more complex model for deeper thinking</param>
    /// <param name="schemaName">Optional schema name for structured output</param>
    /// <param name="jsonSchema">Optional JSON schema for structured output</param>
    /// <returns>The completion response</returns>
    Task<T> GetTypedChatCompletionAsync<T>(List<ChatMessage> messages, bool thinkDeep, string? schemaName, BinaryData? jsonSchema);
}
