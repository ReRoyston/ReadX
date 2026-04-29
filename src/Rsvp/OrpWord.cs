namespace ReadX.Rsvp;

public sealed record OrpWord(string Left, string FocusLetter, string Right, int FocusIndex)
{
    public string Text => Left + FocusLetter + Right;
}
