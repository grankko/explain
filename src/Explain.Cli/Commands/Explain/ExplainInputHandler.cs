using System.Diagnostics;
using System.IO;

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
            string? pipedError = null;
            if (Console.IsInputRedirected)
            {
                var stdinTask = Console.In.ReadToEndAsync();
                Task<string>? errTask = null;
                if (Console.IsErrorRedirected)
                {
                    using var errReader = new StreamReader(Console.OpenStandardError());
                    errTask = errReader.ReadToEndAsync();
                }

                pipedInput = (await stdinTask) ?? string.Empty;
                if (errTask != null)
                    pipedError = await errTask;

                pipedInput = pipedInput.Trim();
                pipedError = pipedError?.Trim();
                result.HasPipedInput = !string.IsNullOrWhiteSpace(pipedInput) || !string.IsNullOrWhiteSpace(pipedError);
            }

            // Determine the content to process
            if (!string.IsNullOrWhiteSpace(pipedInput))
            {
                // If we have piped input, use it as the content to explain
                var combined = pipedInput;
                if (!string.IsNullOrWhiteSpace(pipedError))
                    combined += "\n(stderr):\n" + pipedError;
                result.Content = combined;

                // If there's also a command line question, treat it as a specific question about the piped content
                if (!string.IsNullOrWhiteSpace(args.Question))
                    result.Content = $"Question: {args.Question}\n\nContent to analyze:\n{combined}";
            }
            else if (!string.IsNullOrWhiteSpace(args.Question))
            {
                // No piped input, use the command line question
                result.Content = args.Question;
            }

            // If no piped input and a command is specified, execute it
            if (string.IsNullOrWhiteSpace(pipedInput) && !string.IsNullOrWhiteSpace(args.Command))
            {
                var output = await ExecuteCommandAsync(args.Command, args.CommandArguments);
                if (string.IsNullOrWhiteSpace(output))
                {
                    // Fallback: treat the command as part of the question if there was no output
                    if (string.IsNullOrWhiteSpace(result.Content))
                        result.Content = args.Command;
                }
                else
                {
                    var combined = output;
                    if (!string.IsNullOrWhiteSpace(result.Content))
                        combined = $"Question: {result.Content}\n\nContent to analyze:\n{output}"; // result.Content currently holds question
                    result.Content = combined;
                    result.HasPipedInput = true;
                }
            }

            // Validate input length if we have content
            if (!result.IsEmpty)
            {
                ValidateInputLength(result.Content, args.ThinkDeep, args.IsVerbose);
            }

            return result;
        }

        private static async Task<string> ExecuteCommandAsync(string command, IEnumerable<string> arguments)
        {
            var psi = new ProcessStartInfo(command)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process == null)
                return string.Empty;

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync();

            var output = stdoutTask.Result.Trim();
            var error = stderrTask.Result.Trim();
            if (!string.IsNullOrWhiteSpace(error))
                output += "\n(stderr):\n" + error;

            return output;
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Explain CLI");
            Console.WriteLine(new string('-', 20));
            Console.ResetColor();

            Console.WriteLine("Please provide a question to explain or pipe content to analyze.");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Usage:");
            Console.ResetColor();
            Console.WriteLine("  explain \"your question here\" [--verbose] [--think-deep]");
            Console.WriteLine("  cat file.txt | explain [\"specific question about the content\"] [--verbose] [--think-deep]");
            Console.WriteLine("  explain ls -la [\"question about output\"]");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Input limits:");
            Console.ResetColor();
            Console.WriteLine($"  Regular mode: ~{MAX_INPUT_TOKENS_REGULAR:N0} tokens (~{MAX_INPUT_TOKENS_REGULAR * CHARS_PER_TOKEN / 1024:N0}KB)");
            Console.WriteLine($"  Smart mode:   ~{MAX_INPUT_TOKENS_SMART:N0} tokens (~{MAX_INPUT_TOKENS_SMART * CHARS_PER_TOKEN / 1024:N0}KB)");
            Console.ResetColor();
        }
    }
}
