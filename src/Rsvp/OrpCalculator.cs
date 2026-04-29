namespace ReadX.Rsvp;

public static class OrpCalculator
{
    public static OrpWord Create(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return new OrpWord(string.Empty, string.Empty, string.Empty, 0);
        }

        var focusIndex = GetFocusIndex(word.Length);
        return new OrpWord(
            word[..focusIndex],
            word[focusIndex].ToString(),
            word[(focusIndex + 1)..],
            focusIndex);
    }

    private static int GetFocusIndex(int length) => length switch
    {
        <= 2 => 0,
        <= 5 => 1,
        <= 9 => 2,
        <= 13 => 3,
        _ => 4
    };
}
