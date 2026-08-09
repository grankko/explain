using System.Text;
using System.Text.RegularExpressions;

namespace Explain.Cli.AI;

public static class CitationFormatter
{
    private static readonly Regex MarkdownLinkPattern = new(
        @"\[(?<label>[^\]\r\n]+)\]\((?<url>https?://[^)\r\n]+)\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex ParenthesizedMarkersPattern = new(
        @"\(\s*(?<markers>(?:\[\d+\](?:\s*[,;]\s*)?)+)\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MarkerPattern = new(
        @"\[\d+\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SpaceBeforeMarkerPattern = new(
        @"(?<=[.!?])\s+(?=\[\d+\])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static CitationFormattingResult Format(string text, IEnumerable<AiCitation> citations)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(citations);

        var sourceNumbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<AiSource>();
        var occurrences = new List<CitationOccurrence>();

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

            occurrences.Add(new CitationOccurrence(
                citation.StartIndex,
                citation.EndIndex,
                sourceNumber,
                normalizedUrl));
        }

        var citedText = new StringBuilder(text);
        foreach (var occurrenceGroup in occurrences
                     .GroupBy(item => (item.StartIndex, item.EndIndex))
                     .OrderByDescending(group => group.Key.StartIndex))
        {
            var marker = string.Concat(occurrenceGroup
                .Select(item => item.SourceNumber)
                .Distinct()
                .Order()
                .Select(number => $"[{number}]"));

            if (IsMarkdownCitationSpan(text, occurrenceGroup))
            {
                citedText.Remove(occurrenceGroup.Key.StartIndex, occurrenceGroup.Key.EndIndex - occurrenceGroup.Key.StartIndex);
                citedText.Insert(occurrenceGroup.Key.StartIndex, marker);
            }
            else
            {
                citedText.Insert(occurrenceGroup.Key.EndIndex, marker);
            }
        }

        var terminalText = MarkdownLinkPattern.Replace(
            citedText.ToString(),
            match => $"{match.Groups["label"].Value} ({match.Groups["url"].Value})");
        terminalText = ParenthesizedMarkersPattern.Replace(
            terminalText,
            match => string.Concat(MarkerPattern.Matches(match.Groups["markers"].Value)
                .Cast<Match>()
                .Select(marker => marker.Value)));
        terminalText = SpaceBeforeMarkerPattern.Replace(terminalText, string.Empty);

        citedText.Clear();
        citedText.Append(terminalText);

        if (sources.Count > 0)
        {
            citedText.AppendLine();
            citedText.AppendLine();
            citedText.AppendLine("Sources:");
            foreach (var source in sources)
            {
                citedText.AppendLine($"[{source.Number}] {source.Title}");
                citedText.AppendLine($"    {source.Url}");
            }

            citedText.Length -= Environment.NewLine.Length;
        }

        return new CitationFormattingResult(citedText.ToString(), sources);
    }

    private static bool IsMarkdownCitationSpan(
        string text,
        IEnumerable<CitationOccurrence> occurrences)
    {
        var items = occurrences.ToArray();
        var span = text[items[0].StartIndex..items[0].EndIndex].Trim();
        if (span.StartsWith('(') && span.EndsWith(')'))
            span = span[1..^1].Trim();

        var links = MarkdownLinkPattern.Matches(span);
        if (links.Count == 0)
            return false;

        var citedUrls = items.Select(item => item.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!links.Cast<Match>().Any(link =>
                Uri.TryCreate(link.Groups["url"].Value, UriKind.Absolute, out var uri) &&
                citedUrls.Contains(uri.AbsoluteUri)))
        {
            return false;
        }

        var remainder = MarkdownLinkPattern.Replace(span, string.Empty)
            .Trim(' ', '\t', ',', ';');
        return remainder.Length == 0;
    }

    private sealed record CitationOccurrence(
        int StartIndex,
        int EndIndex,
        int SourceNumber,
        string Url);
}

public sealed record CitationFormattingResult(string Text, IReadOnlyList<AiSource> Sources);
