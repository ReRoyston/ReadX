using System.Text.RegularExpressions;

namespace ReadX.Text;

public static partial class TextCleanupService
{
    private const string ParagraphBreakPlaceholder = "\u0001";

    public static string Clean(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var text = ParagraphBreakRegex().Replace(raw, ParagraphBreakPlaceholder);
        text = HyphenatedLineWrapRegex().Replace(text, string.Empty);
        text = SingleLineBreakRegex().Replace(text, " ");
        text = HorizontalWhitespaceRegex().Replace(text, " ");
        text = text.Replace(ParagraphBreakPlaceholder, "\n", StringComparison.Ordinal);
        text = SpaceAroundNewlineRegex().Replace(text, "\n");
        return text.Trim();
    }

    [GeneratedRegex(@"(?<=\p{L})-\s*[\r\n]+\s*(?=\p{L})")]
    private static partial Regex HyphenatedLineWrapRegex();

    [GeneratedRegex(@"(\r?\n\s*){2,}")]
    private static partial Regex ParagraphBreakRegex();

    [GeneratedRegex(@"\r?\n")]
    private static partial Regex SingleLineBreakRegex();

    [GeneratedRegex(@"[^\S\r\n]+")]
    private static partial Regex HorizontalWhitespaceRegex();

    [GeneratedRegex(@" *\n *")]
    private static partial Regex SpaceAroundNewlineRegex();
}
