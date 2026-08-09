using Explain.Cli.AI.Tools;
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
    private const int MaxLocalToolCalls = 4;
    private readonly OpenAiOptions _openAiOptions;
    private readonly IResponsesApiClient _responsesClient;
    private bool _isVerbose;

    public bool IsVerbose
    {
        get => _isVerbose;
        set => _isVerbose = value;
    }

    public OpenAIServiceAgent(IOptions<OpenAiOptions> openAiOptions)
    {
        _openAiOptions = openAiOptions.Value;
        ValidateApiKey(_openAiOptions.ApiKey);
        _responsesClient = new ResponsesApiClient(openAiOptions);
    }

    public OpenAIServiceAgent(
        IOptions<OpenAiOptions> openAiOptions,
        IResponsesApiClient responsesClient)
    {
        _openAiOptions = openAiOptions.Value;
        ValidateApiKey(_openAiOptions.ApiKey);
        _responsesClient = responsesClient;
    }

    private static void ValidateApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is missing. Please update your appsettings.json file with a valid API key.");
        }
    }

    public Task<AiResponse<string>> GetResponseAsync(
        IReadOnlyList<ResponseItem> messages,
        bool thinkDeep,
        ILocalToolSession? localToolSession = null,
        CancellationToken cancellationToken = default)
    {
        return GetTypedResponseAsync<string>(
            messages,
            thinkDeep,
            null,
            null,
            localToolSession,
            cancellationToken);
    }

    public async Task<AiResponse<T>> GetTypedResponseAsync<T>(
        IReadOnlyList<ResponseItem> messages,
        bool thinkDeep,
        string? schemaName,
        BinaryData? jsonSchema,
        ILocalToolSession? localToolSession = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inputItems = messages.ToList();
            var localToolCalls = 0;
            var promptTokens = 0;
            var completionTokens = 0;
            var totalTokens = 0;
            var webSearchUsed = false;

            if (_isVerbose)
            {
                Console.WriteLine($"Reasoning effort: {(thinkDeep ? "high" : "low")}");
                Console.WriteLine("Web search available: yes (model decides when to use it)");
                Console.WriteLine($"Local memory tools available: {(localToolSession is null ? "no" : "yes")}");
                if (!string.IsNullOrWhiteSpace(schemaName) && jsonSchema is not null)
                    Console.WriteLine($"Using structured output with schema: {schemaName}");
            }

            while (true)
            {
                var options = OpenAiResponseRequestFactory.Create(
                    _openAiOptions,
                    inputItems,
                    thinkDeep,
                    schemaName,
                    jsonSchema,
                    localToolSession?.Tools);

                ResponsesApiResult response = await _responsesClient.CreateAsync(options, cancellationToken);
                promptTokens += response.InputTokens;
                completionTokens += response.OutputTokens;
                totalTokens += response.TotalTokens;
                webSearchUsed |= response.WebSearchUsed;
                inputItems.AddRange(response.OutputItems);

                var functionCalls = response.OutputItems.OfType<FunctionCallResponseItem>().ToList();
                if (functionCalls.Count == 0)
                {
                    return MapResponse<T>(
                        response,
                        schemaName,
                        jsonSchema,
                        promptTokens,
                        completionTokens,
                        totalTokens,
                        webSearchUsed);
                }

                if (localToolSession is null)
                    throw new InvalidOperationException("The model requested a local tool when local tools were disabled.");

                foreach (var functionCall in functionCalls)
                {
                    localToolCalls++;
                    if (localToolCalls > MaxLocalToolCalls)
                        throw new InvalidOperationException($"The response exceeded the limit of {MaxLocalToolCalls} local memory tool calls.");

                    var output = await localToolSession.ExecuteAsync(functionCall, cancellationToken);
                    inputItems.Add(new FunctionCallOutputResponseItem(functionCall.CallId, output));
                }
            }
        }
        catch (Exception ex)
        {
            if (_isVerbose)
                Console.WriteLine($"Error in GetResponse: {ex.Message}");

            throw;
        }
    }

    private AiResponse<T> MapResponse<T>(
        ResponsesApiResult response,
        string? schemaName,
        BinaryData? jsonSchema,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        bool webSearchUsed)
    {
        var attemptedStructuredOutput = !string.IsNullOrWhiteSpace(schemaName) && jsonSchema is not null;
        var aiResult = OpenAiResponseMapper.Map<T>(
            response.Text,
            response.Model,
            promptTokens,
            completionTokens,
            totalTokens,
            webSearchUsed,
            response.Citations,
            attemptedStructuredOutput);

        if (_isVerbose)
        {
            Console.WriteLine($"Web search used: {(webSearchUsed ? "yes" : "no")}");
            Console.WriteLine($"Sources returned: {aiResult.Sources.Count}");
        }

        return aiResult;
    }

}
#pragma warning restore OPENAI001
