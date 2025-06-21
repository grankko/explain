using Explain.Cli.Commands.Explain;

namespace Explain.Cli.Tests.UnitTests.Commands.Explain;

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
    public void ParseArguments_WithShowHistory_SetsShowHistoryTrue()
    {
        // Arrange
        var args = new[] { "--show-history" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(5, result.HistoryLimit); // Default value
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithShowHistoryAndNumber_SetsCorrectLimit()
    {
        // Arrange
        var args = new[] { "--show-history", "10" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(10, result.HistoryLimit);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithShowHistoryAndZero_SetsZeroLimit()
    {
        // Arrange
        var args = new[] { "--show-history", "0" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(0, result.HistoryLimit);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithShowHistoryAndInvalidNumber_UsesDefaultLimit()
    {
        // Arrange
        var args = new[] { "--show-history", "invalid" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(5, result.HistoryLimit); // Default value
        Assert.AreEqual("invalid", result.Question); // Invalid number becomes part of question
    }

    [TestMethod]
    public void ParseArguments_WithShowHistoryAndNegativeNumber_UsesNegativeNumber()
    {
        // Arrange
        var args = new[] { "--show-history", "-5" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(-5, result.HistoryLimit);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithShowHistoryAndQuestion_SetsQuestionCorrectly()
    {
        // Arrange
        var args = new[] { "--show-history", "what", "is", "AI" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(5, result.HistoryLimit); // Default since "what" is not a valid number
        Assert.AreEqual("what is AI", result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithShowHistoryAndOtherFlags_SetsAllFlags()
    {
        // Arrange
        var args = new[] { "--show-history", "15", "--verbose", "--think" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(15, result.HistoryLimit);
        Assert.IsTrue(result.IsVerbose);
        Assert.IsTrue(result.ThinkDeep);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithShowHistoryInMiddle_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "--verbose", "--show-history", "3", "--think", "some", "question" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(3, result.HistoryLimit);
        Assert.IsTrue(result.IsVerbose);
        Assert.IsTrue(result.ThinkDeep);
        Assert.AreEqual("some question", result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithShowHistoryAtEnd_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "what", "is", "this", "--show-history", "7" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(7, result.HistoryLimit);
        Assert.AreEqual("what is this", result.Question);
    }

    [TestMethod]
    public void ParseArguments_DefaultValues_AreCorrect()
    {
        // Arrange
        var args = new[] { "simple", "question" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsFalse(result.ShowHistory);
        Assert.AreEqual(5, result.HistoryLimit); // Default value even when not using --show-history
        Assert.IsFalse(result.IncludeHistory);
        Assert.AreEqual(3, result.IncludeHistoryLimit); // Default value even when not using --include-history
        Assert.IsFalse(result.IsVerbose);
        Assert.IsFalse(result.ThinkDeep);
        Assert.AreEqual("simple question", result.Question);
    }

    [TestMethod]
    public void ParseArguments_EmptyArgs_ReturnsDefaults()
    {
        // Arrange
        var args = new string[0];
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsFalse(result.ShowHistory);
        Assert.AreEqual(5, result.HistoryLimit);
        Assert.IsFalse(result.IncludeHistory);
        Assert.AreEqual(3, result.IncludeHistoryLimit);
        Assert.IsFalse(result.IsVerbose);
        Assert.IsFalse(result.ThinkDeep);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_NullArgs_ReturnsDefaults()
    {
        // Arrange
        string[]? args = null;
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args!);
        
        // Assert
        Assert.IsFalse(result.ShowHistory);
        Assert.AreEqual(5, result.HistoryLimit);
        Assert.IsFalse(result.IncludeHistory);
        Assert.AreEqual(3, result.IncludeHistoryLimit);
        Assert.IsFalse(result.IsVerbose);
        Assert.IsFalse(result.ThinkDeep);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithClearHistory_SetsClearHistoryTrue()
    {
        // Arrange
        var args = new[] { "--clear-history" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ClearHistory);
        Assert.AreEqual(string.Empty, result.Question);
        Assert.IsFalse(result.ShowHistory);
    }

    [TestMethod]
    public void ParseArguments_WithClearHistoryAndOtherFlags_SetsAllFlags()
    {
        // Arrange
        var args = new[] { "--clear-history", "--verbose", "--think" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ClearHistory);
        Assert.IsTrue(result.IsVerbose);
        Assert.IsTrue(result.ThinkDeep);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithClearHistoryInMiddle_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "--verbose", "--clear-history", "--think", "some", "question" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ClearHistory);
        Assert.IsTrue(result.IsVerbose);
        Assert.IsTrue(result.ThinkDeep);
        Assert.AreEqual("some question", result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithClearHistoryAtEnd_ParsesCorrectly()
    {
        // Arrange
        var args = new[] { "what", "is", "this", "--clear-history" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ClearHistory);
        Assert.AreEqual("what is this", result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithBothHistoryFlags_SetsBothFlags()
    {
        // Arrange
        var args = new[] { "--show-history", "10", "--clear-history" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(10, result.HistoryLimit);
        Assert.IsTrue(result.ClearHistory);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_DefaultClearHistory_IsFalse()
    {
        // Arrange
        var args = new[] { "simple", "question" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsFalse(result.ClearHistory);
        Assert.AreEqual("simple question", result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithIncludeHistory_SetsIncludeHistoryTrue()
    {
        // Arrange
        var args = new[] { "--include-history" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.IncludeHistory);
        Assert.AreEqual(3, result.IncludeHistoryLimit); // Default value
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithIncludeHistoryAndNumber_SetsCorrectLimit()
    {
        // Arrange
        var args = new[] { "--include-history", "5" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.IncludeHistory);
        Assert.AreEqual(5, result.IncludeHistoryLimit);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithIncludeHistoryAndZero_SetsZeroLimit()
    {
        // Arrange
        var args = new[] { "--include-history", "0" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.IncludeHistory);
        Assert.AreEqual(0, result.IncludeHistoryLimit);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithIncludeHistoryAndInvalidNumber_UsesDefaultLimit()
    {
        // Arrange
        var args = new[] { "--include-history", "invalid" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.IncludeHistory);
        Assert.AreEqual(3, result.IncludeHistoryLimit); // Default value
        Assert.AreEqual("invalid", result.Question); // Invalid number becomes part of question
    }

    [TestMethod]
    public void ParseArguments_WithIncludeHistoryAndQuestion_SetsQuestionCorrectly()
    {
        // Arrange
        var args = new[] { "--include-history", "what", "is", "AI" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.IncludeHistory);
        Assert.AreEqual(3, result.IncludeHistoryLimit); // Default since "what" is not a valid number
        Assert.AreEqual("what is AI", result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithIncludeHistoryAndOtherFlags_SetsAllFlags()
    {
        // Arrange
        var args = new[] { "--include-history", "7", "--verbose", "--think" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.IncludeHistory);
        Assert.AreEqual(7, result.IncludeHistoryLimit);
        Assert.IsTrue(result.IsVerbose);
        Assert.IsTrue(result.ThinkDeep);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_WithIncludeHistoryAndShowHistory_SetsBothFlags()
    {
        // Arrange
        var args = new[] { "--include-history", "2", "--show-history", "10" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsTrue(result.IncludeHistory);
        Assert.AreEqual(2, result.IncludeHistoryLimit);
        Assert.IsTrue(result.ShowHistory);
        Assert.AreEqual(10, result.HistoryLimit);
        Assert.AreEqual(string.Empty, result.Question);
    }

    [TestMethod]
    public void ParseArguments_DefaultIncludeHistory_IsFalse()
    {
        // Arrange
        var args = new[] { "simple", "question" };
        
        // Act
        var result = ExplainArgumentParser.ParseArguments(args);
        
        // Assert
        Assert.IsFalse(result.IncludeHistory);
        Assert.AreEqual(3, result.IncludeHistoryLimit); // Default value
        Assert.AreEqual("simple question", result.Question);
    }
}
