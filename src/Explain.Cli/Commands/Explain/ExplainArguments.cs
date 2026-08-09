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
        public bool ShowHistory { get; set; } = false;
        public int HistoryLimit { get; set; } = 5;
        public bool ClearHistory { get; set; } = false;
        public bool IncludeHistory { get; set; } = false;
        public int IncludeHistoryLimit { get; set; } = 3;
        public bool NoHistory { get; set; }
        public bool ShowMemory { get; set; }
        public bool ClearMemory { get; set; }
        public bool RefreshSystem { get; set; }
        public bool ShowHelp { get; set; }
    }
}
