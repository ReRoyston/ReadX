using System.Text.RegularExpressions;

namespace ReadX.Tokenization;

public static partial class WordSplitter
{
    public static IReadOnlyList<string> Split(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        var rejoined = HyphenatedLineWrapRegex().Replace(raw, string.Empty);

        return WhitespaceRegex()
            .Split(rejoined.Trim())
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToArray();
    }

    [GeneratedRegex(@"(?<=\p{L})-\s*[\r\n]+\s*(?=\p{L})")]
    private static partial Regex HyphenatedLineWrapRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
