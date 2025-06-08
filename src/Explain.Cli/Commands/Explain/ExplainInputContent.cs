namespace Explain.Cli.Commands.Explain
{
    /// <summary>
    /// Represents the input content to be processed by the explain command.
    /// Separates piped content from argument content for better processing and presentation.
    /// </summary>
    public class ExplainInputContent
    {
        /// <summary>
        /// Content received from piped input (stdin)
        /// </summary>
        public string PipedContent { get; set; } = string.Empty;

        /// <summary>
        /// Content received from command line arguments (question)
        /// </summary>
        public string ArgumentContent { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether any piped input was received
        /// </summary>
        public bool HasPipedInput => !string.IsNullOrWhiteSpace(PipedContent);

        /// <summary>
        /// Indicates whether any argument input was received
        /// </summary>
        public bool HasArgumentInput => !string.IsNullOrWhiteSpace(ArgumentContent);

        /// <summary>
        /// Indicates whether the input content is completely empty
        /// </summary>
        public bool IsEmpty => !HasPipedInput && !HasArgumentInput;

        /// <summary>
        /// [DEPRECATED] Maintains backward compatibility with existing code.
        /// Use ComposeForAI() instead for intelligent prompt composition.
        /// </summary>
        public string Content => ComposeForAI();

        /// <summary>
        /// Composes the final prompt for AI processing, intelligently combining piped and argument content.
        /// </summary>
        /// <returns>Formatted content for AI consumption</returns>
        public virtual string ComposeForAI()
        {
            if (IsEmpty)
                return string.Empty;

            if (HasPipedInput && HasArgumentInput)
            {
                // Both piped content and question - format for context analysis
                return $"Piped content:\n{PipedContent}\n{new string('-', 10)}\nQuestion: {ArgumentContent}";
            }
            else if (HasPipedInput)
            {
                // Only piped content - format for general explanation
                return $"Piped content:\n{PipedContent}";
            }
            else
            {
                // Only argument content - format as question
                return $"Question: {ArgumentContent}";
            }
        }

        /// <summary>
        /// Gets the raw user input suitable for history storage (without AI prompt formatting).
        /// </summary>
        /// <returns>Raw user input for historical records</returns>
        public string GetRawUserInput()
        {
            if (IsEmpty)
                return string.Empty;

            if (HasPipedInput && HasArgumentInput)
            {
                // Both - combine but without AI formatting
                return $"{PipedContent}\n[Question: {ArgumentContent}]";
            }
            else if (HasPipedInput)
            {
                // Only piped content
                return PipedContent;
            }
            else
            {
                // Only argument content
                return ArgumentContent;
            }
        }
    }
}
