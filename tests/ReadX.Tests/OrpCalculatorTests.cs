using ReadX.Rsvp;

namespace ReadX.Tests;

public sealed class OrpCalculatorTests
{
    [Theory]
    [InlineData("I", 0)]
    [InlineData("to", 0)]
    [InlineData("read", 1)]
    [InlineData("reading", 2)]
    [InlineData("recognition", 3)]
    [InlineData("internationalization", 4)]
    public void Create_UsesClassicApproximateFocusIndex(string word, int expectedIndex)
    {
        var result = OrpCalculator.Create(word);

        Assert.Equal(expectedIndex, result.FocusIndex);
        Assert.Equal(word[expectedIndex].ToString(), result.FocusLetter);
    }
}
