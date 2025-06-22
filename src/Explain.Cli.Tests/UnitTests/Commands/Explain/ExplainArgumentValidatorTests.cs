using Explain.Cli.Commands.Explain;

namespace Explain.Cli.Tests.UnitTests.Commands.Explain;

[TestClass]
public class ExplainArgumentValidatorTests
{
    [TestMethod]
    public void ValidateShowHistory_WithValidShowHistoryOnly_ReturnsTrue()
    {
        // Arrange
        var args = new ExplainArguments
        {
            ShowHistory = true,
            Question = "",
            ThinkDeep = false,
            ClearHistory = false
        };

        // Act
        var result = ExplainArgumentValidator.ValidateShowHistory(args);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateShowHistory_WithQuestionAndShowHistory_ReturnsFalse()
    {
        // Arrange
        var args = new ExplainArguments
        {
            ShowHistory = true,
            Question = "What is this?",
            ThinkDeep = false,
            ClearHistory = false
        };

        // Act
        var result = ExplainArgumentValidator.ValidateShowHistory(args);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ValidateClearHistory_WithValidClearHistoryOnly_ReturnsTrue()
    {
        // Arrange
        var args = new ExplainArguments
        {
            ClearHistory = true,
            Question = "",
            ThinkDeep = false,
            ShowHistory = false
        };

        // Act
        var result = ExplainArgumentValidator.ValidateClearHistory(args);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ValidateClearHistory_WithShowHistoryAndClearHistory_ReturnsFalse()
    {
        // Arrange
        var args = new ExplainArguments
        {
            ClearHistory = true,
            Question = "",
            ThinkDeep = false,
            ShowHistory = true
        };

        // Act
        var result = ExplainArgumentValidator.ValidateClearHistory(args);

        // Assert
        Assert.IsFalse(result);
    }
}
