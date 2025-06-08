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
        Assert.IsTrue(result.Content.Contains(smallQuestion)); // Content should contain the question
        Assert.IsTrue(result.Content.StartsWith("Question: ")); // Should be formatted as a question
        Assert.IsFalse(result.HasPipedInput);
    }
    
    [TestMethod]
    public async Task ProcessInputAsync_ArgumentOnly_PopulatesArgumentContentCorrectly()
    {
        // Arrange
        var question = "What is the meaning of life?";
        var args = new ExplainArguments { Question = question, ThinkDeep = false };
        
        // Act
        var result = await ExplainInputHandler.ProcessInputAsync(args);
        
        // Assert
        Assert.IsFalse(result.IsEmpty);
        Assert.IsFalse(result.HasPipedInput);
        Assert.IsTrue(result.HasArgumentInput);
        Assert.AreEqual(string.Empty, result.PipedContent);
        Assert.AreEqual(question, result.ArgumentContent);
        Assert.AreEqual($"Question: {question}", result.ComposeForAI());
        Assert.AreEqual(question, result.GetRawUserInput());
    }
    
    [TestMethod]
    public void ExplainInputContent_SeparatedFields_WorkCorrectly()
    {
        // Test empty content
        var emptyContent = new ExplainInputContent();
        Assert.IsTrue(emptyContent.IsEmpty);
        Assert.IsFalse(emptyContent.HasPipedInput);
        Assert.IsFalse(emptyContent.HasArgumentInput);
        Assert.AreEqual(string.Empty, emptyContent.ComposeForAI());
        Assert.AreEqual(string.Empty, emptyContent.GetRawUserInput());
        
        // Test piped content only
        var pipedOnly = new ExplainInputContent { PipedContent = "console.log('hello')" };
        Assert.IsFalse(pipedOnly.IsEmpty);
        Assert.IsTrue(pipedOnly.HasPipedInput);
        Assert.IsFalse(pipedOnly.HasArgumentInput);
        Assert.AreEqual("Piped content:\nconsole.log('hello')", pipedOnly.ComposeForAI());
        Assert.AreEqual("console.log('hello')", pipedOnly.GetRawUserInput());
        
        // Test argument content only
        var argumentOnly = new ExplainInputContent { ArgumentContent = "explain this code" };
        Assert.IsFalse(argumentOnly.IsEmpty);
        Assert.IsFalse(argumentOnly.HasPipedInput);
        Assert.IsTrue(argumentOnly.HasArgumentInput);
        Assert.AreEqual("Question: explain this code", argumentOnly.ComposeForAI());
        Assert.AreEqual("explain this code", argumentOnly.GetRawUserInput());
        
        // Test both piped and argument content
        var bothContent = new ExplainInputContent 
        { 
            PipedContent = "console.log('hello')", 
            ArgumentContent = "what does this do?" 
        };
        Assert.IsFalse(bothContent.IsEmpty);
        Assert.IsTrue(bothContent.HasPipedInput);
        Assert.IsTrue(bothContent.HasArgumentInput);
        Assert.AreEqual("Piped content:\nconsole.log('hello')\n----------\nQuestion: what does this do?", bothContent.ComposeForAI());
        Assert.AreEqual("console.log('hello')\n[Question: what does this do?]", bothContent.GetRawUserInput());
    }
}
