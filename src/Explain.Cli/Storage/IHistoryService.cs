namespace Explain.Cli.Storage
{
    public interface IHistoryService
    {
        void AddToHistory(string input, string output, string modelName, int promptTokens, int completionTokens, int totalTokens);
        public string GetHistoryAsText(int limit = 5);
    }
}