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
        private readonly IConfigurationDisplayService _configurationDisplayService;

        public ExplainCommand(IOpenAIServiceAgent openAiServiceAgent, IConfigurationDisplayService configurationDisplayService)
        {
            _openAiServiceAgent = openAiServiceAgent;
            _configurationDisplayService = configurationDisplayService;
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
                    ShowVerboseInformation(inputContent, parsedArgs);

                string aiResponse = await GenerateAiResponse(inputContent, parsedArgs);

                Console.WriteLine($"AI Response: {aiResponse}");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }

        private async Task<string> GenerateAiResponse(ExplainInputContent inputContent, ExplainArguments parsedArgs)
        {
            var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(Prompts.ExplainPrompt),
                    new UserChatMessage(inputContent.Content)
                };

            var aiResponse = await _openAiServiceAgent.GetChatCompletionAsync(messages, parsedArgs.ThinkDeep);
            return aiResponse;
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