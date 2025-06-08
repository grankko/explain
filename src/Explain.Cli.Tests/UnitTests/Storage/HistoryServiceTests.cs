using Explain.Cli.Configuration;
using Explain.Cli.Storage;
using Microsoft.Extensions.Options;

namespace Explain.Cli.Tests.UnitTests.Storage;

[TestClass]
public class HistoryServiceTests
{
    private HistoryService _historyService = null!;
    private string _testDatabasePath = null!;

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
}
