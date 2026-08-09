using OpenAI.Responses;

namespace Explain.Cli.AI;

#pragma warning disable OPENAI001
public sealed record ResponsesApiResult(
    string? Text,
    string Model,
    int InputTokens,
    int OutputTokens,
    int TotalTokens,
    bool WebSearchUsed,
    IReadOnlyList<ResponseItem> OutputItems,
    IReadOnlyList<AiCitation> Citations);

public interface IResponsesApiClient
{
    Task<ResponsesApiResult> CreateAsync(
        CreateResponseOptions options,
        CancellationToken cancellationToken = default);
}
#pragma warning restore OPENAI001
