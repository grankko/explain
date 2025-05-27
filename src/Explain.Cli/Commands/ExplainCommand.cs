using Explain.Cli.AI;
using Explain.Cli.Commands.Explain;
using OpenAI.Chat;

namespace Explain.Cli.Commands
{
    /// <summary>
    /// Command implementation for explaining content using AI.
    /// </summary>
    public class ExplainCommand : ICommand
    {
        private readonly IOpenAIServiceAgent _openAiServiceAgent;

        public ExplainCommand(IOpenAIServiceAgent openAiServiceAgent)
        {
            _openAiServiceAgent = openAiServiceAgent;
        }

        public async Task<int> ExecuteAsync(string[] args)
        {
            try
            {
                // Parse command line arguments
                var parsedArgs = ExplainArgumentParser.ParseArguments(args);

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
                    ShowVerboseInformation(inputContent);

                // TODO: Implement the actual AI service call
                Console.WriteLine($"Processing content: {inputContent.Content}");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private static void ShowVerboseInformation(ExplainInputContent inputContent)
        {
            Console.WriteLine($"Content to explain: {inputContent.Content}");
            Console.WriteLine("Verbose mode enabled.");

            if (inputContent.HasPipedInput)
            {
                Console.WriteLine("Input received from pipe.");
            }
        }
    }
}