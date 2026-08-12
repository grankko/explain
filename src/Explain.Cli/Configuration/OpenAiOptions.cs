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
        /// OpenAI model to use when no per-request model is supplied.
        /// </summary>  
        public string DefaultModelName { get; set; } = "gpt-5.6-terra";

        /// <summary>
        /// OpenAI reasoning effort to use when no per-request effort is supplied.
        /// </summary>  
        public string DefaultReasoningEffort { get; set; } = "medium";

        /// <summary>
        /// Optional: Organization ID
        /// </summary>
        public string? Organization { get; set; }
    }
}
