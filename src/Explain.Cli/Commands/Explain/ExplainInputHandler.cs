namespace Explain.Cli.Commands.Explain
{
    /// <summary>
    /// Represents the input content to be processed by the explain command.
    /// </summary>
    public class ExplainInputContent
    {
        public string Content { get; set; } = string.Empty;
        public bool HasPipedInput { get; set; } = false;
        public bool IsEmpty => string.IsNullOrWhiteSpace(Content);
    }

    /// <summary>
    /// Handles input processing for both piped content and command line arguments for the explain command.
    /// </summary>
    public static class ExplainInputHandler
    {
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
            if (Console.IsInputRedirected)
            {
                pipedInput = await Console.In.ReadToEndAsync();
                pipedInput = pipedInput?.Trim();
                result.HasPipedInput = !string.IsNullOrWhiteSpace(pipedInput);
            }

            // Determine the content to process
            if (!string.IsNullOrWhiteSpace(pipedInput))
            {
                // If we have piped input, use it as the content to explain
                result.Content = pipedInput;
                
                // If there's also a command line question, treat it as a specific question about the piped content
                if (!string.IsNullOrWhiteSpace(args.Question))
                    result.Content = $"Question: {args.Question}\n\nContent to analyze:\n{pipedInput}";
            }
            else if (!string.IsNullOrWhiteSpace(args.Question))
            {
                // No piped input, use the command line question
                result.Content = args.Question;
            }

            return result;
        }

        /// <summary>
        /// Displays usage information for the explain command to the user.
        /// </summary>
        public static void ShowUsage()
        {
            Console.WriteLine("Please provide a question to explain or pipe content to analyze.");
            Console.WriteLine("Usage: ");
            Console.WriteLine("  explain \"your question here\" [--verbose]");
            Console.WriteLine("  cat file.txt | explain [\"specific question about the content\"] [--verbose]");
        }
    }
}
