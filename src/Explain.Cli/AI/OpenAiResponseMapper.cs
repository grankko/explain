using System.Text.Json;

namespace Explain.Cli.AI;

public static class OpenAiResponseMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AiResponse<T> Map<T>(
        string? rawText,
        string modelName,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        bool webSearchUsed,
        IEnumerable<AiCitation> citations,
        bool structuredOutput)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            throw new InvalidOperationException("Received empty response from OpenAI.");

        var formatted = CitationFormatter.Format(rawText, citations);

        AiResponse<T> result;
        if (structuredOutput)
        {
            var typedResponse = JsonSerializer.Deserialize<T>(rawText, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize OpenAI response.");
            result = new AiResponse<T>(typedResponse);
        }
        else if (typeof(T) == typeof(string))
        {
            result = new AiResponse<T>((T)(object)formatted.Text);
        }
        else
        {
            throw new InvalidOperationException($"Cannot convert response to type {typeof(T).Name}.");
        }

        result.ModelName = modelName;
        result.PromptTokens = promptTokens;
        result.CompletionTokens = completionTokens;
        result.TotalTokens = totalTokens;
        result.WebSearchUsed = webSearchUsed;
        result.Sources = formatted.Sources;

        return result;
    }
}
