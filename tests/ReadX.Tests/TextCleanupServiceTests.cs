using ReadX.Text;

namespace ReadX.Tests;

public sealed class TextCleanupServiceTests
{
    [Fact]
    public void Clean_RejoinsHyphenatedLineWrapAndCollapsesWhitespace()
    {
        var clean = TextCleanupService.Clean(" recom-\r\n mend   this\r\nsentence ");

        Assert.Equal("recommend this sentence", clean);
    }

    [Fact]
    public void Clean_PreservesParagraphBreaksAsSingleReadableBreak()
    {
        var clean = TextCleanupService.Clean("First paragraph.\r\n\r\nSecond paragraph.");

        Assert.Equal("First paragraph.\nSecond paragraph.", clean);
    }
}
