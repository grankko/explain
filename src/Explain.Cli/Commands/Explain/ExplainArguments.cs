namespace Explain.Cli.Commands.Explain
{
    /// <summary>
    /// Represents parsed command line arguments for the explain command.
    /// </summary>
    public class ExplainArguments
    {
        public string Question { get; set; } = string.Empty;
        public bool IsVerbose { get; set; } = false;
    }
}
