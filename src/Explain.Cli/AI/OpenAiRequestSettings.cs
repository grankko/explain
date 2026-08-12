using OpenAI.Responses;

namespace Explain.Cli.AI;

#pragma warning disable OPENAI001
/// <summary>
/// The model and reasoning effort selected for one Responses API request.
/// </summary>
public sealed record OpenAiRequestSettings(
    string ModelName,
    ResponseReasoningEffortLevel? ReasoningEffort);
#pragma warning restore OPENAI001
