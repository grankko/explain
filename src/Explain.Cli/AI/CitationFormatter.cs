using System.Text;

namespace Explain.Cli.AI;

public static class CitationFormatter
{
    public static CitationFormattingResult Format(string text, IEnumerable<AiCitation> citations)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(citations);

        var sourceNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<AiSource>();
        var insertions = new Dictionary<int, SortedSet<int>>();

        foreach (var citation in citations)
        {
            if (!Uri.TryCreate(citation.Url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                continue;
            }

            var normalizedUrl = uri.AbsoluteUri;
            if (!sourceNumbers.TryGetValue(normalizedUrl, out var sourceNumber))
            {
                sourceNumber = sources.Count + 1;
                sourceNumbers[normalizedUrl] = sourceNumber;
                sources.Add(new AiSource(
                    sourceNumber,
                    string.IsNullOrWhiteSpace(citation.Title) ? normalizedUrl : citation.Title.Trim(),
                    normalizedUrl));
            }

            if (citation.StartIndex < 0 ||
                citation.EndIndex < citation.StartIndex ||
                citation.EndIndex > text.Length)
            {
                continue;
            }

            if (!insertions.TryGetValue(citation.EndIndex, out var markers))
            {
                markers = [];
                insertions[citation.EndIndex] = markers;
            }

            markers.Add(sourceNumber);
        }

        var citedText = new StringBuilder(text);
        foreach (var insertion in insertions.OrderByDescending(item => item.Key))
        {
            var marker = string.Concat(insertion.Value.Select(number => $"[{number}]"));
            citedText.Insert(insertion.Key, marker);
        }

        if (sources.Count > 0)
        {
            citedText.AppendLine();
            citedText.AppendLine();
            citedText.AppendLine("Sources:");
            foreach (var source in sources)
                citedText.AppendLine($"[{source.Number}] {source.Title} - {source.Url}");

            citedText.Length -= Environment.NewLine.Length;
        }

        return new CitationFormattingResult(citedText.ToString(), sources);
    }
}

public sealed record CitationFormattingResult(string Text, IReadOnlyList<AiSource> Sources);
