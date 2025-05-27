namespace Explain.Cli.Configuration
{

#nullable enable

    /// <summary>
    /// Configuration settings for OpenAI integration
    /// </summary>
    public class OpenAiOptions
    {
        public const string SectionName = "OpenAi";

        /// <summary>
        /// OpenAI API Key
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// OpenAI Model to use (e.g., "gpt-4", "gpt-3.5-turbo")
        /// </summary>  
        public string ModelName { get; set; } = "gpt-4";

        /// <summary>
        /// OpenAI Model to use for deep thinking(e.g., "o3", "o4-mini")
        /// </summary>  
        public string SmartModelName { get; set; } = "o4-mini";

        /// <summary>
        /// Optional: Organization ID
        /// </summary>
        public string? Organization { get; set; }
    }
}