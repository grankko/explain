using Explain.Cli.AI;
using Explain.Cli.Commands.Explain;
using Explain.Cli.Configuration;
using Explain.Cli.Extensions;
using Explain.Cli.Storage;
using OpenAI.Chat;

namespace Explain.Cli.Commands
{
    /// <summary>
    /// Command implementation for explaining content using AI.
    /// </summary>
    public class ExplainCommand : ICommand, IDisposable
    {
        private readonly IOpenAIServiceAgent _openAiServiceAgent;
        private readonly IHistoryService _historyService;
        private readonly IConfigurationDisplayService _configurationDisplayService;
        private readonly CancellationTokenSource _animationCts = new();
        private bool _disposed = false;

        public ExplainCommand(IOpenAIServiceAgent openAiServiceAgent, IHistoryService historyService, IConfigurationDisplayService configurationDisplayService)
        {
            _openAiServiceAgent = openAiServiceAgent;
            _historyService = historyService;
            _configurationDisplayService = configurationDisplayService;
        }

        public async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                // Parse command line arguments
                var parsedArgs = ExplainArgumentParser.ParseArguments(args);

                // Handle show history request
                if (parsedArgs.ShowHistory)
                {
                    // Validate that no other input is provided when showing history
                    if (!ExplainArgumentValidator.ValidateShowHistory(parsedArgs))
                    {
                        Console.Out.WriteError("Error: --show-history cannot be combined with other input or flags.");
                        return 1;
                    }

                    // Display history
                    var historyEntries = _historyService.GetLatestHistory(parsedArgs.HistoryLimit);
                    if (historyEntries.Count == 0)
                    {
                        Console.WriteLine("No history found.");
                    }
                    else
                    {
                        historyEntries.DisplayColorized();
                    }
                    return 0;
                }

                // Handle clear history request
                if (parsedArgs.ClearHistory)
                {
                    // Validate that no other input is provided when clearing history
                    if (!ExplainArgumentValidator.ValidateClearHistory(parsedArgs))
                    {
                        Console.Out.WriteError("Error: --clear-history cannot be combined with other input or flags.");
                        return 1;
                    }

                    return HandleClearHistory();
                }

                // Fetch history only if --include-history flag is present
                var history = string.Empty;
                if (parsedArgs.IncludeHistory)
                {
                    var historyForAI = _historyService.GetLatestHistory(parsedArgs.IncludeHistoryLimit);
                    history = historyForAI.FormatForAI();
                }

                // Process input from both command line and piped sources
                var inputContent = await ExplainInputHandler.ProcessInputAsync(parsedArgs);

                // Validate that we have content to process
                if (inputContent.IsEmpty)
                {
                    ExplainInputHandler.ShowUsage();
                    return 1;
                }

                // Configure the AI service
                _openAiServiceAgent.IsVerbose = parsedArgs.IsVerbose;

                // Show verbose information if requested
                if (parsedArgs.IsVerbose)
                    ShowVerboseInformation(inputContent, parsedArgs);

                // Create input content with history prepended for AI processing
                var aiInputContent = CreateInputContentWithHistory(inputContent, history);
                
                var aiResponse = await GenerateAiResponse(aiInputContent, parsedArgs);

                // Output the AI response in purple color
                Console.Out.WriteAiResponse(aiResponse.Response);

                // Save to history using the raw user input (without AI prompt formatting)
                _historyService.AddToHistory(inputContent.GetRawUserInput(), aiResponse.Response, aiResponse.ModelName, aiResponse.PromptTokens, aiResponse.CompletionTokens, aiResponse.TotalTokens);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Handles the clear history command with user confirmation.
        /// </summary>
        /// <returns>Exit code (0 for success, 1 for failure)</returns>
        private int HandleClearHistory()
        {
            // Prompt for confirmation
            Console.WriteLine("This will permanently delete all history entries.");
            Console.Write("Are you sure you want to continue? Type 'yes' to confirm: ");
            var confirmation = Console.ReadLine();
            
            if (string.Equals(confirmation?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
            {
                _historyService.ClearHistory();
                Console.Out.WriteSuccess("History cleared successfully.");
            }
            else
            {
                Console.WriteLine("Operation cancelled.");
            }
            return 0;
        }

        private async Task<AiResponse<string>> GenerateAiResponse(ExplainInputContent inputContent, ExplainArguments parsedArgs)
        {
            var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(Prompts.ExplainPrompt),
                    new SystemChatMessage($"The current time is: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"),
                    new UserChatMessage(inputContent.Content)
                };

            // Start the thinking animation
            var label = "thinking..";
            if (parsedArgs.ThinkDeep) 
                label = "thinking deeply..";

            var animationTask = Console.Out.ShowThinkingAnimationAsync(_animationCts.Token, label);

            try
            {
                var aiResponse = await _openAiServiceAgent.GetChatCompletionAsync(messages, parsedArgs.ThinkDeep);
                return aiResponse;
            }
            finally
            {
                // Stop the animation
                _animationCts.Cancel();
                try
                {
                    await animationTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when animation is canceled
                }
                Console.WriteLine(); // Add a newline after the animation
            }
        }

        /// <summary>
        /// Creates an input content instance with history prepended for AI processing.
        /// </summary>
        /// <param name="originalInput">The original input content</param>
        /// <param name="history">The history text to prepend</param>
        /// <returns>Input content with history included for AI processing</returns>
        private ExplainInputContent CreateInputContentWithHistory(ExplainInputContent originalInput, string history)
        {
            // If no history, return the original input
            if (string.IsNullOrWhiteSpace(history))
                return originalInput;
                
            // Create new content with history prepended to the AI composition
            var contentWithHistory = new ExplainInputContent
            {
                PipedContent = originalInput.PipedContent,
                ArgumentContent = originalInput.ArgumentContent
            };
            
            // Override the ComposeForAI method result by creating a wrapper
            return new ExplainInputContentWithHistory(contentWithHistory, history);
        }

        private void ShowVerboseInformation(ExplainInputContent inputContent, ExplainArguments parsedArgs)
        {
            Console.WriteLine("Verbose mode enabled.");

            if (parsedArgs.ThinkDeep)
                Console.WriteLine("Think deep mode enabled."); 

            _configurationDisplayService.DisplayConfiguration();

            if (inputContent.HasPipedInput)
            {
                Console.WriteLine("Input received from pipe.");
                Console.WriteLine($"Piped content: {inputContent.PipedContent}");
            }
            
            if (inputContent.HasArgumentInput)
            {
                Console.WriteLine($"Argument content: {inputContent.ArgumentContent}");
            }
            
            Console.WriteLine($"Composed content for AI: {inputContent.ComposeForAI()}");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _animationCts?.Dispose();
                _disposed = true;
            }
        }
    }
}