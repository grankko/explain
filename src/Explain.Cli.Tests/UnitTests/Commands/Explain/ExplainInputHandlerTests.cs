using Explain.Cli.Commands.Explain;

namespace Explain.Cli.Tests.UnitTests.Commands.Explain;

[TestClass]
public class ExplainInputHandlerTests
{
    [TestMethod]
    public async Task ProcessInputAsync_LargeInputRegularMode_ThrowsException()
    {
        // Arrange - Create input that exceeds regular mode token limits (100K tokens)
        var largeQuestion = new string('x', 500_000); // ~125K tokens (4 chars per token)
        var args = new ExplainArguments { Question = largeQuestion, ThinkDeep = false };
        
        // Act & Assert
        var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => ExplainInputHandler.ProcessInputAsync(args));
        
        Assert.IsTrue(exception.Message.Contains("too large"));
    }

    [TestMethod]
    public async Task ProcessInputAsync_SmallInputSmartMode_ProcessesSuccessfully()
    {
        // Arrange - Create input that fits comfortably in smart mode (100K tokens max)
        var smallQuestion = new string('x', 200_000); // ~50K tokens - well under both limits
        var args = new ExplainArguments { Question = smallQuestion, ThinkDeep = true };
        
        // Act
        var result = await ExplainInputHandler.ProcessInputAsync(args);
        
        // Assert
        Assert.IsFalse(result.IsEmpty);
        Assert.AreEqual(smallQuestion, result.Content);
        Assert.IsFalse(result.HasPipedInput);
    }
}
