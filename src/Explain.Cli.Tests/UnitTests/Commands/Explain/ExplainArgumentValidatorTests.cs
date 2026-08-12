using Explain.Cli.Commands.Explain;

namespace Explain.Cli.Tests.UnitTests.Commands.Explain;

[TestClass]
public class ExplainArgumentValidatorTests
{
    [TestMethod]
    public void GetValidationError_NoHistoryAndIncludeHistory_ReturnsError()
    {
        var result = ExplainArgumentValidator.GetValidationError(new ExplainArguments
        {
            NoHistory = true,
            IncludeHistory = true
        });

        StringAssert.Contains(result, "cannot be combined");
    }

    [TestMethod]
    public void GetValidationError_NegativeHistoryLimit_ReturnsError()
    {
        var result = ExplainArgumentValidator.GetValidationError(new ExplainArguments
        {
            IncludeHistoryLimit = -1
        });

        StringAssert.Contains(result, "cannot be negative");
    }

    [TestMethod]
    public void GetValidationError_MultipleManagementCommands_ReturnsError()
    {
        var result = ExplainArgumentValidator.GetValidationError(new ExplainArguments
        {
            ShowMemory = true,
            ClearHistory = true
        });

        StringAssert.Contains(result, "cannot be combined");
    }

    [TestMethod]
    public void ValidateShowHistory_WithValidShowHistoryOnly_ReturnsTrue()
    {
        // Arrange
        var args = new ExplainArguments
        {
            ShowHistory = true,
            Question = "",
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
            ShowHistory = true
        };

        // Act
        var result = ExplainArgumentValidator.ValidateClearHistory(args);

        // Assert
        Assert.IsFalse(result);
    }
}
