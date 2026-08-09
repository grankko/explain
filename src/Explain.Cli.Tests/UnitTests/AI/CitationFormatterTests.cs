using Explain.Cli.AI;

namespace Explain.Cli.Tests.UnitTests.AI;

[TestClass]
public class CitationFormatterTests
{
    [TestMethod]
    public void Format_MultipleCitations_AddsMarkersAndSources()
    {
        var result = CitationFormatter.Format(
            "Alpha and beta.",
            [
                new AiCitation("Alpha source", "https://example.com/alpha", 0, 5),
                new AiCitation("Beta source", "https://example.com/beta", 10, 14)
            ]);

        Assert.AreEqual(
            "Alpha[1] and beta[2].\n\nSources:\n[1] Alpha source - https://example.com/alpha\n[2] Beta source - https://example.com/beta",
            result.Text.ReplaceLineEndings("\n"));
        Assert.HasCount(2, result.Sources);
    }

    [TestMethod]
    public void Format_DuplicateUrls_ReusesSourceNumber()
    {
        var result = CitationFormatter.Format(
            "First and second.",
            [
                new AiCitation("Source", "https://example.com/item", 0, 5),
                new AiCitation("Source again", "https://example.com/item", 10, 16)
            ]);

        StringAssert.Contains(result.Text, "First[1]");
        StringAssert.Contains(result.Text, "second[1]");
        Assert.HasCount(1, result.Sources);
    }

    [TestMethod]
    public void Format_UnicodeText_PreservesCharactersAndMarkerPosition()
    {
        var result = CitationFormatter.Format(
            "Café is open.",
            [new AiCitation("Café", "https://example.com/cafe", 0, 4)]);

        StringAssert.StartsWith(result.Text, "Café[1] is open.");
    }

    [TestMethod]
    public void Format_InvalidRange_StillListsValidSource()
    {
        var result = CitationFormatter.Format(
            "Short answer.",
            [new AiCitation("Source", "https://example.com/source", 0, 999)]);

        StringAssert.StartsWith(result.Text, "Short answer.\n\nSources:");
        Assert.HasCount(1, result.Sources);
    }

    [TestMethod]
    public void Format_NoCitations_ReturnsUnchangedText()
    {
        var result = CitationFormatter.Format("No search needed.", []);

        Assert.AreEqual("No search needed.", result.Text);
        Assert.IsEmpty(result.Sources);
    }
}
