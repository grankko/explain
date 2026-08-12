using Explain.Cli.Extensions;

namespace Explain.Cli.Commands.Explain
{

    /// <summary>
    /// Handles input processing for both piped content and command line arguments for the explain command.
    /// </summary>
    public static class ExplainInputHandler
    {
        // Token limits for safety (leaving room for system prompt + response)
        private const int MaxInputTokens = 100_000; // ~400KB text
        private const int CHARS_PER_TOKEN = 4; // Rough estimate

        /// <summary>
        /// Processes input from both piped content and command line arguments for the explain command.
        /// </summary>
        /// <param name="args">Parsed explain command arguments</param>
        /// <returns>Processed input content with separated piped and argument content</returns>
        public static async Task<ExplainInputContent> ProcessInputAsync(ExplainArguments args)
        {
            var result = new ExplainInputContent();
            
            // Check if input is being piped
            string? pipedInput = null;
            // Only try to read piped input if we're not in a test environment
            // Test environments often have redirected input but no actual piped content
            if (Console.IsInputRedirected && !IsTestEnvironment())
            {
                try
                {
                    pipedInput = await Console.In.ReadToEndAsync();
                    pipedInput = pipedInput?.Trim();
                }
                catch
                {
                    // If reading fails, treat as no piped input
                }
            }
            
            // Populate separate content fields
            if (!string.IsNullOrWhiteSpace(pipedInput))
                result.PipedContent = pipedInput;
                
            if (!string.IsNullOrWhiteSpace(args.Question))
                result.ArgumentContent = args.Question;

            // Validate input length if we have content
            if (!result.IsEmpty)
                ValidateInputLength(result.ComposeForAI(), args.IsVerbose);

            return result;
        }

        /// <summary>
        /// Validates that the input content doesn't exceed token limits for the selected model.
        /// </summary>
        /// <param name="content">The content to validate</param>
        /// <param name="isVerbose">Whether verbose output is enabled</param>
        /// <exception cref="InvalidOperationException">Thrown when input exceeds token limits</exception>
        private static void ValidateInputLength(string content, bool isVerbose)
        {
            var contentLength = content.Length;
            var maxChars = MaxInputTokens * CHARS_PER_TOKEN;

            if (contentLength > maxChars)
            {
                var actualTokensEstimate = contentLength / CHARS_PER_TOKEN;
                throw new InvalidOperationException(
                    $"Input too large: ~{actualTokensEstimate:N0} tokens (max: {MaxInputTokens:N0}). " +
                    $"Consider reducing input size or splitting into smaller chunks.");
            }

            if (isVerbose)
            {
                var estimatedTokens = contentLength / CHARS_PER_TOKEN;
                Console.WriteLine($"Estimated input tokens: ~{estimatedTokens:N0} (limit: {MaxInputTokens:N0})");
            }
        }

        /// <summary>
        /// Displays usage information for the explain command to the user.
        /// </summary>
        public static void ShowUsage()
        {
            Console.Out.WriteHeader("Explain CLI");
            Console.Out.WriteHeader(new string('-', 20));

            Console.WriteLine("Please provide a question to explain or pipe content to analyze.");
            Console.WriteLine();

            Console.Out.WriteInfo("Usage:");
            Console.WriteLine("  explain \"your question here\" [--model name] [--reasoning effort] [--verbose]");
            Console.WriteLine("  cat file.txt | explain [\"specific question about the content\"] [--model name] [--reasoning effort] [--verbose]");
            Console.WriteLine("  explain --show-history [number]");
            Console.WriteLine("  explain --clear-history");
            Console.WriteLine("  explain --show-memory");
            Console.WriteLine("  explain --clear-memory");
            Console.WriteLine("  explain --refresh-system [\"optional question\"]");
            Console.WriteLine("  explain --help");
            Console.WriteLine("  explain --version");
            Console.WriteLine();

            Console.Out.WriteInfo("Options:");
            WriteOption("--verbose", "Show detailed configuration and processing information");
            WriteOption("--model name", "Use this OpenAI model for this request");
            WriteOption("--reasoning effort", "Use none, minimal, low, medium, high, or xhigh for this request");
            WriteOption("--include-history [n]", "Override automatic recent history with the last n entries (default: 3)");
            WriteOption("--no-history", "Ephemeral request: do not load or save history or memory");
            WriteOption("--show-history [n]", "Show last n history entries (default: 5, cannot be combined with other input)");
            WriteOption("--clear-history", "Clear all history (requires confirmation, cannot be combined with other input)");
            WriteOption("--show-memory", "Show system, global, and project memories");
            WriteOption("--clear-memory", "Clear AI-created memories while retaining system facts");
            WriteOption("--refresh-system", "Re-collect the local OS, kernel, architecture, shell, and time zone.");
            WriteOptionContinuation("It updates only reserved local system facts; no hostname, username, network details,");
            WriteOptionContinuation("environment values, or hardware identifiers are collected. It does not make an AI request.");
            WriteOption("-h, --help", "Show this help without making an AI request");
            WriteOption("-V, --version", "Show the installed Explain version");
            Console.WriteLine();

            Console.Out.WriteInfo("Input limits:");
            Console.WriteLine($"  All models: ~{MaxInputTokens:N0} tokens (~{MaxInputTokens * CHARS_PER_TOKEN / 1024:N0}KB)");
        }

        private static void WriteOption(string option, string description)
        {
            Console.WriteLine($"  {option,-22} {description}");
        }

        private static void WriteOptionContinuation(string description)
        {
            Console.WriteLine($"  {string.Empty,-22} {description}");
        }

        /// <summary>
        /// Detects if we're running in a test environment to avoid hanging on console input.
        /// </summary>
        /// <returns>True if running in test environment</returns>
        private static bool IsTestEnvironment()
        {
            // Check for common test environment indicators
            return AppDomain.CurrentDomain.GetAssemblies().Any(a => 
                a.FullName?.Contains("Microsoft.VisualStudio.TestPlatform") == true ||
                a.FullName?.Contains("Microsoft.TestPlatform") == true ||
                a.FullName?.Contains("nunit") == true ||
                a.FullName?.Contains("xunit") == true ||
                a.FullName?.Contains("MSTest") == true);
        }
    }
}
