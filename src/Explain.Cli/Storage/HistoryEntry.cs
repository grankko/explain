namespace Explain.Cli.Storage;

public sealed class HistoryEntry
{
    public long Id { get; init; }
    public DateTimeOffset RecordedAtUtc { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public bool HadPipedInput { get; init; }
    public int PipedCharacterCount { get; init; }
    public string OutputText { get; init; } = string.Empty;
    public string ModelName { get; init; } = string.Empty;
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens { get; init; }
}
