using Explain.Cli.Extensions;

namespace Explain.Cli.Memory;

public static class MemoryDisplayExtensions
{
    public static void DisplayColorized(this IReadOnlyList<MemoryRecord> memories)
    {
        Console.Out.WriteHeader("=== Memory ===");
        foreach (var memory in memories)
        {
            var scope = memory.Scope == MemoryScope.Project
                ? $"project: {memory.ScopeKey}"
                : memory.Scope.ToString().ToLowerInvariant();
            Console.Out.WriteInfo($"[{scope}] {memory.Key} ({memory.Category}, updated {memory.UpdatedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss})");
            Console.Out.WriteLabelAndContent("Value: ", memory.Value, ConsoleColor.Cyan);
            Console.Out.WriteSeparator();
        }
    }
}
