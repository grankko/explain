using Explain.Cli.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace Explain.Cli.AI;

#pragma warning disable OPENAI001
/// <summary>
/// Agent for interacting with the OpenAI Responses API.
/// </summary>
public class OpenAIServiceAgent : IOpenAIServiceAgent
{
    private readonly OpenAiOptions _openAiOptions;
    private readonly ResponsesClient _responsesClient;
    private bool _isVerbose;

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

        _responsesClient = new ResponsesClient(_openAiOptions.ApiKey);
    }

    public Task<AiResponse<string>> GetResponseAsync(IReadOnlyList<ResponseItem> messages, bool thinkDeep)
    {
        return GetTypedResponseAsync<string>(messages, thinkDeep, null, null);
    }

    public async Task<AiResponse<T>> GetTypedResponseAsync<T>(
        IReadOnlyList<ResponseItem> messages,
        bool thinkDeep,
        string? schemaName,
        BinaryData? jsonSchema)
    {
        try
        {
            var options = OpenAiResponseRequestFactory.Create(
                _openAiOptions,
                messages,
                thinkDeep,
                schemaName,
                jsonSchema);

            if (_isVerbose)
            {
                Console.WriteLine($"Reasoning effort: {(thinkDeep ? "high" : "low")}");
                Console.WriteLine("Web search available: yes (model decides when to use it)");
                if (!string.IsNullOrWhiteSpace(schemaName) && jsonSchema is not null)
                    Console.WriteLine($"Using structured output with schema: {schemaName}");
            }

            ResponseResult response = await _responsesClient.CreateResponseAsync(options);
            return MapResponse<T>(response, schemaName, jsonSchema);
        }
        catch (Exception ex)
        {
            if (_isVerbose)
                Console.WriteLine($"Error in GetResponse: {ex.Message}");

            throw;
        }
    }

    private AiResponse<T> MapResponse<T>(ResponseResult response, string? schemaName, BinaryData? jsonSchema)
    {
        var rawText = response.GetOutputText();
        var webSearchUsed = response.OutputItems.Any(item => item is WebSearchCallResponseItem);
        var citations = ExtractCitations(response);
        var attemptedStructuredOutput = !string.IsNullOrWhiteSpace(schemaName) && jsonSchema is not null;
        var aiResult = OpenAiResponseMapper.Map<T>(
            rawText,
            response.Model,
            response.Usage.InputTokenCount,
            response.Usage.OutputTokenCount,
            response.Usage.TotalTokenCount,
            webSearchUsed,
            citations,
            attemptedStructuredOutput);

        if (_isVerbose)
        {
            Console.WriteLine($"Web search used: {(webSearchUsed ? "yes" : "no")}");
            Console.WriteLine($"Sources returned: {aiResult.Sources.Count}");
        }

        return aiResult;
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
