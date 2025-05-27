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

        /// <sum    mary>
        /// OpenAI M    odel to use (e.g., "gpt-4", "gpt-3.5-turbo")
        /// </summary>  
        public string ModelName { get; set; } = "gpt-4";

        /// <summary>
        /// Optional: Organization ID
        /// </summary>
        public string? Organization { get; set; }
    }
}