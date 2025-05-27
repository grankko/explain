namespace Explain.Cli.Commands
{
    /// <summary>
    /// Interface for CLI commands.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Execute the command
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Exit code (0 for success, non-zero for error)</returns>
        Task<int> ExecuteAsync(string[] args);
    }
}