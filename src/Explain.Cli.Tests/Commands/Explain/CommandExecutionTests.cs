using Explain.Cli.Commands.Explain;

namespace Explain.Cli.Tests.Commands.Explain;

[TestClass]
public class CommandExecutionTests
{
    [TestMethod]
    public async Task ProcessInputAsync_CommandExecution_CapturesStdoutAndStderr()
    {
        var args = new ExplainArguments { Command = "sh" };
        args.CommandArguments.AddRange(new[] { "-c", "echo out && echo err 1>&2" });

        var result = await ExplainInputHandler.ProcessInputAsync(args);

        Assert.IsTrue(result.Content.Contains("out"));
        Assert.IsTrue(result.Content.Contains("err"));
        Assert.IsTrue(result.HasPipedInput);
    }

    [TestMethod]
    public async Task ProcessInputAsync_CommandWithQuestion_FormatsContent()
    {
        var args = new ExplainArguments { Command = "sh", Question = "What is this?" };
        args.CommandArguments.AddRange(new[] { "-c", "echo data" });

        var result = await ExplainInputHandler.ProcessInputAsync(args);

        Assert.IsTrue(result.Content.StartsWith("Question: What is this?"));
        Assert.IsTrue(result.Content.Contains("data"));
    }
}
