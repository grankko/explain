using System.Text;
using Explain.Cli.Extensions;

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
            Console.Out.WriteHeader("=== History ===");

            foreach (var entry in historyEntries)
            {
                // Display timestamp and model info
                Console.Out.WriteInfo($"[{entry.RecordedAt:yyyy-MM-dd HH:mm:ss}] Model: {entry.ModelName}");

                // Display input with label
                Console.Out.WriteLabelAndContent("Input: ", entry.InputText, ConsoleColor.Cyan);

                // Display output with label (using same color as AI responses)
                Console.Out.WriteLabelAndContent("Output: ", entry.OutputText, ConsoleColor.Cyan, ConsoleColor.Magenta);

                // Separator line
                Console.Out.WriteSeparator();
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
