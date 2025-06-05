namespace Explain.Cli.Commands.Explain
{
    /// <summary>
    /// Represents parsed command line arguments for the explain command.
    /// </summary>
    public class ExplainArguments
    {
        public string Question { get; set; } = string.Empty;
        public bool IsVerbose { get; set; } = false;
        public bool ThinkDeep { get; set; } = false;

        /// <summary>
        /// Optional command to execute when no piped input is provided.
        /// </summary>
        public string? Command { get; set; }

        /// <summary>
        /// Arguments for the command to execute.
        /// </summary>
        public List<string> CommandArguments { get; } = new();
    }
}
