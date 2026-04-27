using ReadX.Tokenization;

namespace ReadX.Text;

public sealed record TextPipelineResult(string RawText, string ProcessedText, IReadOnlyList<string> Words);

public static class TextPipeline
{
    public static TextPipelineResult BuildWords(string rawText, bool cleanupEnabled)
    {
        var processed = cleanupEnabled ? TextCleanupService.Clean(rawText) : rawText;
        var words = WordSplitter.Split(processed);
        return new TextPipelineResult(rawText, processed, words);
    }
}
