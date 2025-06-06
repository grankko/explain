using Explain.Cli.Configuration;
using Explain.Cli.Storage;
using Microsoft.Extensions.Options;

namespace Explain.Cli.Tests.UnitTests.Storage;

[TestClass]
public class HistoryServiceTests
{
    private HistoryService _historyService;
    private string _testDatabasePath;

    [TestInitialize]
    public void Setup()
    {
        // Create a unique test database file for each test
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"test_history_{Guid.NewGuid()}.sqlite");
        var storageOptions = Options.Create(new StorageOptions
        {
            ConnectionString = $"Data Source={_testDatabasePath}"
        });
        _historyService = new HistoryService(storageOptions);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up test database file
        if (File.Exists(_testDatabasePath))
        {
            File.Delete(_testDatabasePath);
        }
    }

    [TestMethod]
    public void ClearHistory_WithEmptyDatabase_DoesNotThrow()
    {
        // Arrange - database is already empty after initialization
        
        // Act & Assert
        _historyService.ClearHistory();
    }

    [TestMethod]
    public void ClearHistory_WithExistingEntries_RemovesAllEntries()
    {
        // Arrange
        _historyService.AddToHistory("Question 1", "Answer 1", "gpt-4", 10, 20, 30);
        _historyService.AddToHistory("Question 2", "Answer 2", "gpt-4", 15, 25, 40);
        _historyService.AddToHistory("Question 3", "Answer 3", "gpt-4", 12, 18, 30);
        
        // Verify we have entries before clearing
        var historyBeforeClear = _historyService.GetHistoryAsText(10);
        Assert.IsFalse(string.IsNullOrWhiteSpace(historyBeforeClear));
        
        // Act
        _historyService.ClearHistory();
        
        // Assert
        var historyAfterClear = _historyService.GetHistoryAsText(10);
        Assert.IsTrue(string.IsNullOrWhiteSpace(historyAfterClear));
    }

    [TestMethod]
    public void ClearHistory_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        _historyService.AddToHistory("Question 1", "Answer 1", "gpt-4", 10, 20, 30);
        
        // Act - clear multiple times
        _historyService.ClearHistory();
        _historyService.ClearHistory();
        _historyService.ClearHistory();
        
        // Assert - should not throw and history should still be empty
        var history = _historyService.GetHistoryAsText(10);
        Assert.IsTrue(string.IsNullOrWhiteSpace(history));
    }

    [TestMethod]
    public void ClearHistory_ThenAddNewEntry_WorksCorrectly()
    {
        // Arrange
        _historyService.AddToHistory("Original Question", "Original Answer", "gpt-4", 10, 20, 30);
        
        // Act
        _historyService.ClearHistory();
        _historyService.AddToHistory("New Question", "New Answer", "gpt-4", 5, 10, 15);
        
        // Assert
        var history = _historyService.GetHistoryAsText(10);
        Assert.IsFalse(string.IsNullOrWhiteSpace(history));
        Assert.IsTrue(history.Contains("New Question"));
        Assert.IsTrue(history.Contains("New Answer"));
        Assert.IsFalse(history.Contains("Original Question"));
        Assert.IsFalse(history.Contains("Original Answer"));
    }
}
