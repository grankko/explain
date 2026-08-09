using Explain.Cli.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace Explain.Cli.AI;

#pragma warning disable OPENAI001
public sealed class ResponsesApiClient : IResponsesApiClient
{
    private readonly ResponsesClient _client;

    public ResponsesApiClient(IOptions<OpenAiOptions> options)
    {
        _client = new ResponsesClient(options.Value.ApiKey);
    }

    public async Task<ResponsesApiResult> CreateAsync(
        CreateResponseOptions options,
        CancellationToken cancellationToken = default)
    {
        ResponseResult response = await _client.CreateResponseAsync(options, cancellationToken);
        return new ResponsesApiResult(
            response.GetOutputText(),
            response.Model,
            response.Usage.InputTokenCount,
            response.Usage.OutputTokenCount,
            response.Usage.TotalTokenCount,
            response.OutputItems.Any(item => item is WebSearchCallResponseItem),
            response.OutputItems.ToArray(),
            ExtractCitations(response).ToArray());
    }

    private static IEnumerable<AiCitation> ExtractCitations(ResponseResult response)
    {
        var textOffset = 0;
        foreach (var message in response.OutputItems.OfType<MessageResponseItem>())
        {
            foreach (var contentPart in message.Content)
            {
                var partText = contentPart.Text ?? string.Empty;
                foreach (var annotation in contentPart.OutputTextAnnotations.OfType<UriCitationMessageAnnotation>())
                {
                    yield return new AiCitation(
                        annotation.Title,
                        annotation.Uri.ToString(),
                        textOffset + annotation.StartIndex,
                        textOffset + annotation.EndIndex);
                }
                textOffset += partText.Length;
            }
        }
    }
}
#pragma warning restore OPENAI001
