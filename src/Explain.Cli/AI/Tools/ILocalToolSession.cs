using Explain.Cli.Memory;
using OpenAI.Responses;

namespace Explain.Cli.AI.Tools;

#pragma warning disable OPENAI001
public interface ILocalToolSession
{
    IReadOnlyList<ResponseTool> Tools { get; }
    Task<string> ExecuteAsync(FunctionCallResponseItem functionCall, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MemoryChange>> CommitAsync(CancellationToken cancellationToken = default);
}
#pragma warning restore OPENAI001
