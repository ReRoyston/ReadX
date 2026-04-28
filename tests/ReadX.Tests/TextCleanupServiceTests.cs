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

    [Fact]
    public void Clean_DoesNotRejoinHyphenAcrossParagraphBreak()
    {
        var clean = TextCleanupService.Clean("First-\r\n\r\nSecond");

        Assert.Equal("First-\nSecond", clean);
    }

    [Fact]
    public void Clean_PreservesExistingControlCharacters()
    {
        var clean = TextCleanupService.Clean("A\u0001B");

        Assert.Equal("A\u0001B", clean);
    }

    [Fact]
    public void Clean_RejoinsHyphenatedWrapAfterCombiningMark()
    {
        var clean = TextCleanupService.Clean("Cafe\u0301-\nstyle");

        Assert.Equal("Caféstyle", clean);
    }
}
