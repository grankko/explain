namespace Explain.Cli.Commands.Explain;

/// <summary>
/// Validates explain command arguments and ensures proper combinations.
/// </summary>
public static class ExplainArgumentValidator
{
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
               !parsedArgs.ClearHistory;
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
               !parsedArgs.ShowHistory;
    }
}
