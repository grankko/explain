using Explain.Cli.Commands.Explain;

namespace Explain.Cli.Tests.Commands.Explain;

[TestClass]
public class ExplainArgumentParserTests
{
    [TestMethod]
    public void ParseArguments_WithBothFlags_SetsBothTrue()
    {
        // Arrange
        var args = new[] { "question", "--verbose", "--think" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.IsVerbose);
        Assert.IsTrue(result.ThinkDeep);
        Assert.AreEqual("question", result.Question);
    }

    [TestMethod]
    public void ParseArguments_FlagsInDifferentOrder_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "--think", "question", "here", "--verbose" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.AreEqual("question here", result.Question);
        Assert.IsTrue(result.IsVerbose);
        Assert.IsTrue(result.ThinkDeep);
    }

    [TestMethod]
    public void ParseArguments_MultipleQuestionParts_CombinesWithSpaces()
    {
        // Arrange
        var args = new[] { "What", "is", "machine", "learning?" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.AreEqual("What is machine learning?", result.Question);
    }

    [TestMethod]
    public void ParseArguments_CommandDetected_FirstTokenExecutable()
    {
        // Arrange
        var args = new[] { "ls", "-la" };

        // Act
        var result = ExplainArgumentParser.ParseArguments(args);

        // Assert
        Assert.AreEqual("ls", result.Command);
        CollectionAssert.AreEqual(new[] { "-la" }, result.CommandArguments);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_QuestionAndCommand_MixedOrder()
    {
        // Arrange
        var args = new[] { "\"What does this show?\"", "ls" };

        // Act
        var result = ExplainArgumentParser.ParseArguments(args);

        // Assert
        Assert.AreEqual("What does this show?", result.Question);
        Assert.AreEqual("ls", result.Command);
    }
}
