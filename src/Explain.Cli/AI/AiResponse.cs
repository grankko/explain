namespace Explain.Cli.AI;

public class AiResponse<T>
{
    public AiResponse(T response)
    {
        Response = response;
    }

    public T Response { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}