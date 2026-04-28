using System.Windows;
using System.Windows.Input;
using ReadX.Rsvp;

namespace ReadX.Views;

public partial class RsvpOverlay : Window
{
    public RsvpOverlay()
    {
        InitializeComponent();
    }

    public event Action? PauseRequested;
    public event Action? RestartRequested;
    public event Action? CancelRequested;

    public void ShowWord(string word, int index, int total)
    {
        ShowWord(OrpCalculator.Create(word), index, total);
    }

    public void ShowWord(OrpWord word, int index, int total)
    {
        LeftWordText.Text = word.Left;
        FocusLetterText.Text = word.FocusLetter;
        RightWordText.Text = word.Right;
        Progress.Value = total <= 0 ? 0 : (double)index / total;
        FooterText.Text = $"{index} / {total}";
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            PauseRequested?.Invoke();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.R)
        {
            RestartRequested?.Invoke();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelRequested?.Invoke();
            e.Handled = true;
        }
    }
}
