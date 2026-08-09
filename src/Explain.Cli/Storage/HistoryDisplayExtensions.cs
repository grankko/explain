using Explain.Cli.Extensions;

namespace Explain.Cli.Storage;

public static class HistoryDisplayExtensions
{
    public static void DisplayColorized(this IReadOnlyList<HistoryEntry> historyEntries)
    {
        Console.Out.WriteHeader("=== History ===");

        foreach (var entry in historyEntries)
        {
            Console.Out.WriteInfo($"[{entry.RecordedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss zzz}] Model: {entry.ModelName}");
            if (!string.IsNullOrWhiteSpace(entry.QuestionText))
                Console.Out.WriteLabelAndContent("Question: ", entry.QuestionText, ConsoleColor.Cyan);
            if (entry.HadPipedInput)
                Console.Out.WriteInfo($"Piped input: {entry.PipedCharacterCount:N0} characters (content not retained)");
            Console.Out.WriteLabelAndContent("Output: ", entry.OutputText, ConsoleColor.Cyan, ConsoleColor.Magenta);
            Console.Out.WriteSeparator();
        }
    }
}
