using Explain.Cli.Commands.Explain;
using OpenAI.Responses;

namespace Explain.Cli.AI.Context;

#pragma warning disable OPENAI001
public sealed record AiContextResult(
    IReadOnlyList<ResponseItem> Items,
    string ProjectScopeKey,
    IReadOnlyList<string> Warnings);

public interface IAiContextBuilder
{
    Task<AiContextResult> BuildAsync(
        ExplainInputContent input,
        ExplainArguments arguments,
        CancellationToken cancellationToken = default);
}
#pragma warning restore OPENAI001
