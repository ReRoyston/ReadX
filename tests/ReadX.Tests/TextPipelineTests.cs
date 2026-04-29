using ReadX.Text;

namespace ReadX.Tests;

public sealed class TextPipelineTests
{
    [Fact]
    public void BuildWords_WhenCleanupEnabled_UsesCleanedText()
    {
        var result = TextPipeline.BuildWords("recom-\nmend   this", cleanupEnabled: true);

        Assert.Equal("recommend this", result.ProcessedText);
        Assert.Equal(["recommend", "this"], result.Words);
    }

    [Fact]
    public void BuildWords_WhenCleanupDisabled_StillSplitsRawText()
    {
        var result = TextPipeline.BuildWords("one   two", cleanupEnabled: false);

        Assert.Equal("one   two", result.ProcessedText);
        Assert.Equal(["one", "two"], result.Words);
    }
}
