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
