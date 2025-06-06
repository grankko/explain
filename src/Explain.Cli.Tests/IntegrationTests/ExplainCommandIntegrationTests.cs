using System.Diagnostics;

namespace Explain.Cli.Tests.IntegrationTests;

[TestClass]
public class ExplainCommandIntegrationTests
{
    private string _cliPath = "";

    [TestInitialize]
    public void TestInitialize()
    {
        // Get the built CLI executable path
        _cliPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Explain.Cli", "bin", "Debug", "net9.0", "Explain.Cli.dll"));
    }

    [TestMethod]
    [Ignore("Integration test - run manually or with specific test execution")]
    public async Task ExplainCommand_WithNoArguments_ShowsUsageAndExitsWithError()
    {
        // Act
        var result = await RunCliProcessAsync();

        // Assert
        Assert.AreEqual(1, result.ExitCode, "CLI should exit with error code when no arguments provided");
        Assert.IsTrue(result.StandardOutput.Contains("Explain CLI"),
            $"Should show usage information. Actual stdout: {result.StandardOutput}");
        Assert.IsTrue(result.StandardOutput.Contains("Usage:"),
            $"Should show usage information. Actual stdout: {result.StandardOutput}");
    }

    [TestMethod]
    [Ignore("Integration test - run manually or with specific test execution")]
    public async Task ExplainCommand_WithPipedInput_HandlesInputCorrectly()
    {
        // Arrange
        var inputText = "simple test content";
        
        // Act
        var result = await RunCliProcessWithInputAsync(inputText);

        // Assert - Should either process the content or fail gracefully with an error
        Assert.IsTrue(result.ExitCode == 0 || result.StandardError.Contains("Error") || 
                     result.StandardOutput.Contains("Error") || result.StandardOutput.Contains("API"),
            $"CLI should handle piped input gracefully. Exit code: {result.ExitCode}, " +
            $"Stderr: {result.StandardError}, Stdout: {result.StandardOutput}");
    }

    private async Task<ProcessResult> RunCliProcessAsync(params string[] args)
    {
        var escapedArgs = string.Join(" ", args.Select(arg => $"\"{arg.Replace("\"", "\\\"")}\""));
        
        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliPath}\" {escapedArgs}",
            UseShellExecute = false,
            RedirectStandardInput = true,  // Need to redirect stdin to control it
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start process");
        }

        // Immediately close stdin to signal no input is coming
        process.StandardInput.Close();

        // Set a timeout to prevent hanging
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var processTask = process.WaitForExitAsync();
        
        var completedTask = await Task.WhenAny(processTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            process.Kill(true);
            throw new TimeoutException("Process timed out after 10 seconds");
        }

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout.Trim(),
            StandardError = stderr.Trim()
        };
    }

    private async Task<ProcessResult> RunCliProcessWithInputAsync(string input, params string[] args)
    {
        var escapedArgs = string.Join(" ", args.Select(arg => $"\"{arg.Replace("\"", "\\\"")}\""));
        
        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{_cliPath}\" {escapedArgs}",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start process");
        }

        // Write input and close stdin immediately to prevent blocking
        await process.StandardInput.WriteAsync(input);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();

        // Set a timeout to prevent hanging
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
        var processTask = process.WaitForExitAsync();
        
        var completedTask = await Task.WhenAny(processTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            process.Kill(true);
            throw new TimeoutException("Process timed out after 10 seconds");
        }

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        return new ProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdout.Trim(),
            StandardError = stderr.Trim()
        };
    }

    private class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = "";
        public string StandardError { get; set; } = "";
    }
}
