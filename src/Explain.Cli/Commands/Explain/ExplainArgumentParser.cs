using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            if (argsList.Count == 0)
                return result;

            // Separate quoted question parts from potential command tokens
            var questionTokens = new List<string>();
            var commandTokens = new List<string>();
            bool inQuote = false;
            foreach (var token in argsList)
            {
                if (token.StartsWith("\"") || token.StartsWith("'"))
                    inQuote = true;

                if (inQuote)
                {
                    questionTokens.Add(token.Trim('"', '\''));
                    if (token.EndsWith("\"") || token.EndsWith("'"))
                        inQuote = false;
                }
                else
                {
                    commandTokens.Add(token);
                }
            }

            if (questionTokens.Count > 0)
                result.Question = string.Join(" ", questionTokens).Trim();

            if (commandTokens.Count > 0)
            {
                if (IsExecutable(commandTokens[0]))
                {
                    result.Command = commandTokens[0];
                    result.CommandArguments.AddRange(commandTokens.Skip(1));
                }
                else if (commandTokens.Count > 1 && IsExecutable(commandTokens[^1]))
                {
                    result.Command = commandTokens[^1];
                    result.CommandArguments.AddRange(commandTokens.Take(commandTokens.Count - 1));
                }
                else if (string.IsNullOrWhiteSpace(result.Question))
                {
                    result.Question = string.Join(" ", commandTokens).Trim('"', '\'');
                }
            }

            return result;
        }

        private static bool IsExecutable(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (File.Exists(token))
                return true;

            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths)
            {
                var candidate = Path.Combine(path, token);
                if (File.Exists(candidate))
                    return true;
                if (File.Exists(candidate + ".exe"))
                    return true;
            }

            return false;
        }
    }
}
