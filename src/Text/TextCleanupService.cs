using System.Text;
using System.Text.RegularExpressions;

namespace ReadX.Text;

public static partial class TextCleanupService
{
    public static string Clean(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var normalized = raw.Normalize(NormalizationForm.FormC);

        var paragraphs = ParagraphBreakRegex()
            .Split(normalized)
            .Select(CleanParagraph)
            .Where(paragraph => paragraph.Length > 0);

        return string.Join('\n', paragraphs);
    }

    private static string CleanParagraph(string paragraph)
    {
        var text = paragraph;
        text = HyphenatedLineWrapRegex().Replace(text, string.Empty);
        text = SingleLineBreakRegex().Replace(text, " ");
        text = HorizontalWhitespaceRegex().Replace(text, " ");
        return text.Trim();
    }

    [GeneratedRegex(@"(?<=\p{L})-\s*[\r\n]+\s*(?=\p{L})")]
    private static partial Regex HyphenatedLineWrapRegex();

    [GeneratedRegex(@"(?:\r?\n\s*){2,}")]
    private static partial Regex ParagraphBreakRegex();

    [GeneratedRegex(@"\r?\n")]
    private static partial Regex SingleLineBreakRegex();

    [GeneratedRegex(@"[^\S\r\n]+")]
    private static partial Regex HorizontalWhitespaceRegex();

}
