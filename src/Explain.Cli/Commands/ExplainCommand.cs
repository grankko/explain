using Explain.Cli.AI;
using Explain.Cli.Commands.Explain;
using Explain.Cli.Configuration;
using Explain.Cli.Storage;
using OpenAI.Chat;
using System.Threading;

namespace Explain.Cli.Commands
{
    /// <summary>
    /// Command implementation for explaining content using AI.
    /// </summary>
    public class ExplainCommand : ICommand
    {
        private readonly IOpenAIServiceAgent _openAiServiceAgent;
        private readonly IHistoryService _historyService;
        private readonly IConfigurationDisplayService _configurationDisplayService;
        private readonly CancellationTokenSource _animationCts = new();

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
                    if (!string.IsNullOrWhiteSpace(parsedArgs.Question) || parsedArgs.ThinkDeep || parsedArgs.ClearHistory || Console.IsInputRedirected)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: --show-history cannot be combined with other input or flags.");
                        Console.ResetColor();
                        return 1;
                    }

                    // Display history
                    var historyText = _historyService.GetHistoryAsText(parsedArgs.HistoryLimit);
                    if (string.IsNullOrWhiteSpace(historyText))
                    {
                        Console.WriteLine("No history found.");
                    }
                    else
                    {
                        Console.WriteLine(historyText);
                    }
                    return 0;
                }

                // Handle clear history request
                if (parsedArgs.ClearHistory)
                {
                    // Validate that no other input is provided when clearing history
                    if (!string.IsNullOrWhiteSpace(parsedArgs.Question) || parsedArgs.ThinkDeep || parsedArgs.ShowHistory)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error: --clear-history cannot be combined with other input or flags.");
                        Console.ResetColor();
                        return 1;
                    }

                    // Prompt for confirmation
                    Console.WriteLine("This will permanently delete all history entries.");
                    Console.Write("Are you sure you want to continue? Type 'yes' to confirm: ");
                    var confirmation = Console.ReadLine();
                    
                    if (string.Equals(confirmation?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                    {
                        _historyService.ClearHistory();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("History cleared successfully.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("Operation cancelled.");
                    }
                    return 0;
                }

                // Fetch history for normal operation
                var history = _historyService.GetHistoryAsText(5);

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

                // Create a copy of input content for adding history (don't modify the original)
                var inputContentWithHistory = new ExplainInputContent
                {
                    Content = inputContent.Content,
                    HasPipedInput = inputContent.HasPipedInput
                };
                
                if (!string.IsNullOrWhiteSpace(history))
                    inputContentWithHistory.Content = $"{history}\n{inputContent.Content}";
                
                var aiResponse = await GenerateAiResponse(inputContentWithHistory, parsedArgs);

                // Output the AI response in purple color
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(aiResponse.Response);
                Console.ResetColor();

                // Save to history using the original input content (without history prepended)
                _historyService.AddToHistory(inputContent.Content, aiResponse.Response, aiResponse.ModelName, aiResponse.PromptTokens, aiResponse.CompletionTokens, aiResponse.TotalTokens);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
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
            var animationTask = ShowThinkingAnimationAsync(_animationCts.Token);

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

        private async Task ShowThinkingAnimationAsync(CancellationToken cancellationToken)
        {
            string[] spinner = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            int spinnerIndex = 0;

            Console.Write(" "); // Add a space before the animation

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Console.Write($"\r {spinner[spinnerIndex]} thinking...");
                    spinnerIndex = (spinnerIndex + 1) % spinner.Length;
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Clean up the animation line
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1));
            }
        }

        private void ShowVerboseInformation(ExplainInputContent inputContent, ExplainArguments parsedArgs)
        {
            Console.WriteLine("Verbose mode enabled.");

            if (parsedArgs.ThinkDeep)
                Console.WriteLine("Think deep mode enabled."); 

            _configurationDisplayService.DisplayConfiguration();

            Console.WriteLine($"Content to explain: {inputContent.Content}");
            
            if (inputContent.HasPipedInput)
                Console.WriteLine("Input received from pipe.");
        }
    }
}