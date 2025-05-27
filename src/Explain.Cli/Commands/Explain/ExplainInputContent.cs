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
}
