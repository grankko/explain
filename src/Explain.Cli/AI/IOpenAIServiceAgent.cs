using OpenAI.Responses;
using Explain.Cli.AI.Tools;

namespace Explain.Cli.AI;

#pragma warning disable OPENAI001
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
    /// Gets a response from OpenAI.
    /// </summary>
    /// <param name="messages">The chat messages to send</param>
    /// <param name="request">The selected model and reasoning effort.</param>
    /// <returns>The generated response</returns>
    Task<AiResponse<string>> GetResponseAsync(
        IReadOnlyList<ResponseItem> messages,
        OpenAiRequestSettings request,
        ILocalToolSession? localToolSession = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a response from OpenAI with optional structured output.
    /// </summary>
    /// <param name="messages">The chat messages to send</param>
    /// <param name="request">The selected model and reasoning effort.</param>
    /// <param name="schemaName">Optional schema name for structured output</param>
    /// <param name="jsonSchema">Optional JSON schema for structured output</param>
    /// <returns>The generated response</returns>
    Task<AiResponse<T>> GetTypedResponseAsync<T>(
        IReadOnlyList<ResponseItem> messages,
        OpenAiRequestSettings request,
        string? schemaName,
        BinaryData? jsonSchema,
        ILocalToolSession? localToolSession = null,
        CancellationToken cancellationToken = default);
}
#pragma warning restore OPENAI001
