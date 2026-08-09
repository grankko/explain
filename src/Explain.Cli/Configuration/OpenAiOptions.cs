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
        /// OpenAI model to use for regular requests.
        /// </summary>  
        public string ModelName { get; set; } = "gpt-5.6-terra";

        /// <summary>
        /// OpenAI model to use for requests made with --think.
        /// </summary>  
        public string SmartModelName { get; set; } = "gpt-5.6-terra";

        /// <summary>
        /// Optional: Organization ID
        /// </summary>
        public string? Organization { get; set; }
    }
}
