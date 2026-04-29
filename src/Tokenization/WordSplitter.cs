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

        return WhitespaceRegex()
            .Split(raw.Trim())
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToArray();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
