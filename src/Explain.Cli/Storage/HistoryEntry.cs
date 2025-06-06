namespace Explain.Cli.Storage
{
    public class HistoryEntry
    {
        public string InputText { get; set; } = string.Empty;
        public string OutputText { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
        public int Id { get; set; } // This will be set by the database as an auto-incremented value
        public HistoryEntry() { }
        public HistoryEntry(string inputText, string outputText, string modelName, int promptTokens, int completionTokens, int totalTokens)
        {
            InputText = inputText;
            OutputText = outputText;
            ModelName = modelName;
            PromptTokens = promptTokens;
            CompletionTokens = completionTokens;
            TotalTokens = totalTokens;
            RecordedAt = DateTime.UtcNow;
        }
    }
}