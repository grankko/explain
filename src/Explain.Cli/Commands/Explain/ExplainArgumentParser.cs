namespace Explain.Cli.Commands.Explain
{
    /// <summary>
    /// Handles parsing of command line arguments for the explain command.
    /// </summary>
    public static class ExplainArgumentParser
    {
        /// <summary>
        /// Parses command line arguments into a structured format for the explain command.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Parsed explain command arguments</returns>
        public static ExplainArguments ParseArguments(string[] args)
        {
            var result = new ExplainArguments();
            
            if (args == null || args.Length == 0)
            {
                return result;
            }

            var argsList = new List<string>(args);
            
            // Check for verbose flag
            if (argsList.Contains("--verbose"))
            {
                result.IsVerbose = true;
                argsList.Remove("--verbose");
            }

            // Check for think deep flag
            if (argsList.Contains("--think"))
            {
                result.ThinkDeep = true;
                argsList.Remove("--think");
            }

            // Check for show history flag
            var showHistoryIndex = argsList.FindIndex(arg => arg == "--show-history");
            if (showHistoryIndex != -1)
            {
                result.ShowHistory = true;
                argsList.RemoveAt(showHistoryIndex);
                
                // Check if there's a number following --show-history
                if (showHistoryIndex < argsList.Count && int.TryParse(argsList[showHistoryIndex], out var limit))
                {
                    result.HistoryLimit = limit;
                    argsList.RemoveAt(showHistoryIndex);
                }
            }

            // Check for include history flag
            var includeHistoryIndex = argsList.FindIndex(arg => arg == "--include-history");
            if (includeHistoryIndex != -1)
            {
                result.IncludeHistory = true;
                argsList.RemoveAt(includeHistoryIndex);
                
                // Check if there's a number following --include-history
                if (includeHistoryIndex < argsList.Count && int.TryParse(argsList[includeHistoryIndex], out var includeLimit))
                {
                    result.IncludeHistoryLimit = includeLimit;
                    argsList.RemoveAt(includeHistoryIndex);
                }
            }

            // Check for clear history flag
            if (argsList.Contains("--clear-history"))
            {
                result.ClearHistory = true;
                argsList.Remove("--clear-history");
            }

            // The remaining argument should be the question
            if (argsList.Count > 0)
            {
                // If there are multiple arguments remaining, join them as a single question
                result.Question = string.Join(" ", argsList).Trim('"', '\'');
            }

            return result;
        }
    }
}
