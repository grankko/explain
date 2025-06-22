namespace Explain.Cli.Storage
{
    public interface IHistoryService
    {
        void AddToHistory(string input, string output, string modelName, int promptTokens, int completionTokens, int totalTokens);
        List<HistoryEntry> GetLatestHistory(int limit = 5);
        void ClearHistory();
    }
}