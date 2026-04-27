using System.Windows;
using System.Windows.Input;

namespace ReadX.Views;

public partial class RsvpOverlay : Window
{
    public RsvpOverlay()
    {
        InitializeComponent();
    }

    public event Action? PauseRequested;
    public event Action? CancelRequested;

    public void ShowWord(string word, int index, int total)
    {
        WordText.Text = word;
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

        if (e.Key == Key.Escape)
        {
            CancelRequested?.Invoke();
            e.Handled = true;
        }
    }
}
