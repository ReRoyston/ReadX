using ReadX.Tokenization;

namespace ReadX.Tests;

public class WordSplitterTests
{
    [Fact]
    public void Split_ReturnsWhitespaceSeparatedWords()
    {
        var words = WordSplitter.Split("hello world");

        Assert.Equal(["hello", "world"], words);
    }

    [Fact]
    public void Split_TreatsLineWrappedHyphenAsTokenTextWhenCleanupIsSkipped()
    {
        var words = WordSplitter.Split("recom-\nmend reading");

        Assert.Equal(["recom-", "mend", "reading"], words);
    }

    [Fact]
    public void Split_PreservesIntraWordPunctuation()
    {
        var words = WordSplitter.Split("don't stop");

        Assert.Equal(["don't", "stop"], words);
    }

    [Fact]
    public void Split_ReturnsEmptyListForWhitespaceOnlyInput()
    {
        var words = WordSplitter.Split("  \r\n\t ");

        Assert.Empty(words);
    }
}
