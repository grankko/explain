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

            if (argsList.Contains("--help") || argsList.Contains("-h"))
            {
                result.ShowHelp = true;
                argsList.RemoveAll(arg => arg is "--help" or "-h");
            }

            if (argsList.Contains("--version") || argsList.Contains("-V"))
            {
                result.ShowVersion = true;
                argsList.RemoveAll(arg => arg is "--version" or "-V");
            }
            
            // Check for verbose flag
            if (argsList.Contains("--verbose"))
            {
                result.IsVerbose = true;
                argsList.Remove("--verbose");
            }

            ParseValueOption(argsList, "--model", value => result.ModelName = value, result);
            ParseValueOption(argsList, "--reasoning", value => result.ReasoningEffort = value, result);

            if (argsList.Remove("--think"))
                result.ArgumentError = "--think has been replaced by --reasoning. For example: --reasoning high.";

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

            if (argsList.Contains("--no-history"))
            {
                result.NoHistory = true;
                argsList.Remove("--no-history");
            }

            if (argsList.Contains("--show-memory"))
            {
                result.ShowMemory = true;
                argsList.Remove("--show-memory");
            }

            if (argsList.Contains("--clear-memory"))
            {
                result.ClearMemory = true;
                argsList.Remove("--clear-memory");
            }

            if (argsList.Contains("--refresh-system"))
            {
                result.RefreshSystem = true;
                argsList.Remove("--refresh-system");
            }

            // The remaining argument should be the question
            if (argsList.Count > 0)
            {
                // If there are multiple arguments remaining, join them as a single question
                result.Question = string.Join(" ", argsList).Trim('"', '\'');
            }

            return result;
        }

        private static void ParseValueOption(
            List<string> args,
            string option,
            Action<string> setValue,
            ExplainArguments result)
        {
            var index = args.FindIndex(argument => argument == option);
            if (index == -1)
                return;

            args.RemoveAt(index);
            if (index >= args.Count || args[index].StartsWith("--", StringComparison.Ordinal))
            {
                result.ArgumentError ??= $"{option} requires a value.";
                return;
            }

            setValue(args[index]);
            args.RemoveAt(index);

            if (args.Contains(option))
                result.ArgumentError ??= $"{option} can only be specified once.";
        }
    }
}
