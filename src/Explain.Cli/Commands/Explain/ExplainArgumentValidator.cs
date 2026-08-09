namespace Explain.Cli.Commands.Explain;

/// <summary>
/// Validates explain command arguments and ensures proper combinations.
/// </summary>
public static class ExplainArgumentValidator
{
    public static string? GetValidationError(ExplainArguments parsedArgs)
    {
        if (parsedArgs.HistoryLimit < 0 || parsedArgs.IncludeHistoryLimit < 0)
            return "History limits cannot be negative.";
        if (parsedArgs.NoHistory && parsedArgs.IncludeHistory)
            return "--no-history cannot be combined with --include-history.";
        if (parsedArgs.NoHistory && parsedArgs.RefreshSystem)
            return "--no-history cannot be combined with --refresh-system.";

        var managementCommandCount = new[]
        {
            parsedArgs.ShowHistory,
            parsedArgs.ClearHistory,
            parsedArgs.ShowMemory,
            parsedArgs.ClearMemory
        }.Count(value => value);
        if (managementCommandCount > 1)
            return "History and memory management commands cannot be combined.";

        if (managementCommandCount == 1 &&
            (!string.IsNullOrWhiteSpace(parsedArgs.Question) || parsedArgs.ThinkDeep ||
             parsedArgs.IncludeHistory || parsedArgs.NoHistory || parsedArgs.RefreshSystem))
        {
            return "History and memory management commands cannot be combined with a question or response options.";
        }

        return null;
    }

    /// <summary>
    /// Validates that show history is not combined with other inputs or flags.
    /// </summary>
    /// <param name="parsedArgs">The parsed arguments to validate</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public static bool ValidateShowHistory(ExplainArguments parsedArgs)
    {
        if (!parsedArgs.ShowHistory) return true;
        
        return string.IsNullOrWhiteSpace(parsedArgs.Question) && 
               !parsedArgs.ThinkDeep && 
               !parsedArgs.ClearHistory &&
               !parsedArgs.ShowMemory &&
               !parsedArgs.ClearMemory;
    }

    /// <summary>
    /// Validates that clear history is not combined with other inputs or flags.
    /// </summary>
    /// <param name="parsedArgs">The parsed arguments to validate</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public static bool ValidateClearHistory(ExplainArguments parsedArgs)
    {
        if (!parsedArgs.ClearHistory) return true;
        
        return string.IsNullOrWhiteSpace(parsedArgs.Question) && 
               !parsedArgs.ThinkDeep && 
               !parsedArgs.ShowHistory &&
               !parsedArgs.ShowMemory &&
               !parsedArgs.ClearMemory;
    }
}
