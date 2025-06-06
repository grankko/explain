using System.Text;
using Explain.Cli.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Explain.Cli.Storage
{

    public class HistoryService : IHistoryService
    {
        private readonly string _connectionString;

        public HistoryService(IOptions<StorageOptions> options)
        {
            _connectionString = options.Value.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(_connectionString));

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE IF NOT EXISTS History (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                RecordedAt TEXT NOT NULL,
                InputText TEXT NOT NULL,
                OutputText TEXT NOT NULL,
                ModelName TEXT,
                PromptTokens INTEGER,
                CompletionTokens INTEGER,
                TotalTokens INTEGER
            );";
            command.ExecuteNonQuery();
        }

        public void AddToHistory(string input, string output, string modelName, int promptTokens, int completionTokens, int totalTokens)
        {
            // Validate all parameters
            if (string.IsNullOrEmpty(input)) throw new ArgumentException("Input text cannot be null or empty.", nameof(input));
            if (string.IsNullOrEmpty(output)) throw new ArgumentException("Output text cannot be null or empty.", nameof(output));
            if (string.IsNullOrEmpty(modelName)) throw new ArgumentException("Model name cannot be null or empty.", nameof(modelName));
            if (promptTokens < 0) throw new ArgumentOutOfRangeException(nameof(promptTokens), "Prompt tokens cannot be negative.");
            if (completionTokens < 0) throw new ArgumentOutOfRangeException(nameof(completionTokens), "Completion tokens cannot be negative.");
            if (totalTokens < 0) throw new ArgumentOutOfRangeException(nameof(totalTokens), "Total tokens cannot be negative.");

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO History (RecordedAt, InputText, OutputText, ModelName, PromptTokens, CompletionTokens, TotalTokens)
            VALUES ($recordedAt, $inputText, $outputText, $modelName, $promptTokens, $completionTokens, $totalTokens);";

            command.Parameters.AddWithValue("$recordedAt", DateTime.Now.ToString("o"));
            command.Parameters.AddWithValue("$inputText", input);
            command.Parameters.AddWithValue("$outputText", output);
            command.Parameters.AddWithValue("$modelName", modelName);
            command.Parameters.AddWithValue("$promptTokens", promptTokens);
            command.Parameters.AddWithValue("$completionTokens", completionTokens);
            command.Parameters.AddWithValue("$totalTokens", totalTokens);

            command.ExecuteNonQuery();
        }

        public string GetHistoryAsText(int limit = 5)
        {
            var history = GetHistory(limit);
            if (history.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("=== History ===");
            foreach (var entry in history)
            {
                sb.AppendLine($"[{entry.RecordedAt:yyyy-MM-dd HH:mm:ss}] Model: {entry.ModelName}");
                sb.AppendLine($"Input: {entry.InputText}");
                sb.AppendLine($"Output: {entry.OutputText}");
                sb.AppendLine(new string('-', 10));
            }
            return sb.ToString();
        }

        private List<HistoryEntry> GetHistory(int limit = 5)
        {
            var history = new List<HistoryEntry>();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Id, RecordedAt, InputText, OutputText, ModelName, PromptTokens, CompletionTokens, TotalTokens
            FROM History
            ORDER BY RecordedAt DESC
            LIMIT $limit;";
            command.Parameters.AddWithValue("$limit", limit);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                history.Add(new HistoryEntry
                {
                    Id = reader.GetInt32(0),
                    RecordedAt = DateTime.Parse(reader.GetString(1)),
                    InputText = reader.GetString(2),
                    OutputText = reader.GetString(3),
                    ModelName = reader.GetString(4),
                    PromptTokens = reader.GetInt32(5),
                    CompletionTokens = reader.GetInt32(6),
                    TotalTokens = reader.GetInt32(7)
                });
            }

            return history;
        }

        public void ClearHistory()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM History;";
            command.ExecuteNonQuery();
        }
    }
}