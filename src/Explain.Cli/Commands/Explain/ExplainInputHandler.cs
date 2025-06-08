using System.Text;
using Explain.Cli.Extensions;

namespace Explain.Cli.Commands.Explain
{

    /// <summary>
    /// Handles input processing for both piped content and command line arguments for the explain command.
    /// </summary>
    public static class ExplainInputHandler
    {
        // Token limits for safety (leaving room for system prompt + response)
        private const int MAX_INPUT_TOKENS_REGULAR = 100_000; // ~400KB text
        private const int MAX_INPUT_TOKENS_SMART = 100_000;
        private const int CHARS_PER_TOKEN = 4; // Rough estimate

        /// <summary>
        /// Processes input from both piped content and command line arguments for the explain command.
        /// </summary>
        /// <param name="args">Parsed explain command arguments</param>
        /// <returns>Processed input content</returns>
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
                    result.HasPipedInput = !string.IsNullOrWhiteSpace(pipedInput);
                }
                catch
                {
                    // If reading fails, treat as no piped input
                }
            }
            
            var contentBuilder = new StringBuilder();

            // Determine the content to process
            if (!string.IsNullOrWhiteSpace(pipedInput))
            {
                // If we have piped input, use it as the content to explain
                contentBuilder.AppendLine($"Piped content:");
                contentBuilder.AppendLine(pipedInput);
                contentBuilder.AppendLine(new string('-', 10));

                // If there's also a command line question, treat it as a specific question about the piped content
                if (!string.IsNullOrWhiteSpace(args.Question))
                    contentBuilder.AppendLine($"Question: {args.Question}");

            }
            else if (!string.IsNullOrWhiteSpace(args.Question))
                // No piped input, use the command line question
                contentBuilder.AppendLine($"Question: {args.Question}");

            result.Content = contentBuilder.ToString();

            // Validate input length if we have content
            if (!result.IsEmpty)
                ValidateInputLength(result.Content, args.ThinkDeep, args.IsVerbose);

            return result;
        }

        /// <summary>
        /// Validates that the input content doesn't exceed token limits for the selected model.
        /// </summary>
        /// <param name="content">The content to validate</param>
        /// <param name="thinkDeep">Whether smart/deep thinking mode is enabled</param>
        /// <param name="isVerbose">Whether verbose output is enabled</param>
        /// <exception cref="InvalidOperationException">Thrown when input exceeds token limits</exception>
        private static void ValidateInputLength(string content, bool thinkDeep, bool isVerbose)
        {
            var contentLength = content.Length;
            var maxTokens = thinkDeep ? MAX_INPUT_TOKENS_SMART : MAX_INPUT_TOKENS_REGULAR;
            var maxChars = maxTokens * CHARS_PER_TOKEN;

            if (contentLength > maxChars)
            {
                var actualTokensEstimate = contentLength / CHARS_PER_TOKEN;
                var modelType = thinkDeep ? "smart mode" : "regular mode";
                
                throw new InvalidOperationException(
                    $"Input too large: ~{actualTokensEstimate:N0} tokens (max: {maxTokens:N0} for {modelType}). " +
                    $"Consider reducing input size or splitting into smaller chunks.");
            }

            if (isVerbose)
            {
                var estimatedTokens = contentLength / CHARS_PER_TOKEN;
                var modelType = thinkDeep ? "smart mode" : "regular mode";
                Console.WriteLine($"Estimated input tokens: ~{estimatedTokens:N0} (limit: {maxTokens:N0} for {modelType})");
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
            Console.WriteLine("  explain \"your question here\" [--verbose] [--think]");
            Console.WriteLine("  cat file.txt | explain [\"specific question about the content\"] [--verbose] [--think]");
            Console.WriteLine("  explain --show-history [number]");
            Console.WriteLine("  explain --clear-history");
            Console.WriteLine();

            Console.Out.WriteInfo("Options:");
            Console.WriteLine("  --verbose          Show detailed configuration and processing information");
            Console.WriteLine("  --think            Use advanced reasoning with smart models");
            Console.WriteLine("  --show-history [n] Show last n history entries (default: 5, cannot be combined with other input)");
            Console.WriteLine("  --clear-history    Clear all history (requires confirmation, cannot be combined with other input)");
            Console.WriteLine();

            Console.Out.WriteInfo("Input limits:");
            Console.WriteLine($"  Regular mode: ~{MAX_INPUT_TOKENS_REGULAR:N0} tokens (~{MAX_INPUT_TOKENS_REGULAR * CHARS_PER_TOKEN / 1024:N0}KB)");
            Console.WriteLine($"  Smart mode:   ~{MAX_INPUT_TOKENS_SMART:N0} tokens (~{MAX_INPUT_TOKENS_SMART * CHARS_PER_TOKEN / 1024:N0}KB)");
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
