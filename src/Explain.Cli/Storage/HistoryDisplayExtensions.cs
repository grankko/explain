using System.Text;

namespace Explain.Cli.Storage
{
    /// <summary>
    /// Extension methods for displaying and formatting history entries.
    /// </summary>
    public static class HistoryDisplayExtensions
    {
        /// <summary>
        /// Displays history entries with colorized output for the --show-history command.
        /// </summary>
        /// <param name="historyEntries">The history entries to display</param>
        public static void DisplayColorized(this List<HistoryEntry> historyEntries)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("=== History ===");
            Console.ResetColor();

            foreach (var entry in historyEntries)
            {
                // Display timestamp and model info
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[{entry.RecordedAt:yyyy-MM-dd HH:mm:ss}] Model: {entry.ModelName}");
                Console.ResetColor();

                // Display input with label
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Input: ");
                Console.ResetColor();
                Console.WriteLine(entry.InputText);

                // Display output with label (using same color as AI responses)
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Output: ");
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(entry.OutputText);
                Console.ResetColor();

                // Separator line
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(new string('-', 10));
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Formats history entries as plain text for AI context inclusion.
        /// </summary>
        /// <param name="historyEntries">The history entries to format</param>
        /// <returns>Formatted history text for AI consumption</returns>
        public static string FormatForAI(this List<HistoryEntry> historyEntries)
        {
            if (historyEntries.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("=== History ===");
            foreach (var entry in historyEntries)
            {
                sb.AppendLine($"[{entry.RecordedAt:yyyy-MM-dd HH:mm:ss}] Model: {entry.ModelName}");
                sb.AppendLine($"Input: {entry.InputText}");
                sb.AppendLine($"Output: {entry.OutputText}");
                sb.AppendLine(new string('-', 10));
            }
            return sb.ToString();
        }
    }
}
