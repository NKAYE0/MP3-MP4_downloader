using System.Windows;
using System.Windows.Input;

namespace YtTikDownloader.App.Views;

/// <summary>
/// A minimal "type a name" dialog, used for naming a saved preset. WPF has
/// no built-in equivalent of VB's InputBox, so this is a small standalone
/// Window instead of pulling in a dependency for one text field.
/// </summary>
public partial class InputDialog : Window
{
    public string InputText => InputBox.Text.Trim();

    public InputDialog(string title, string prompt, string initialValue = "")
    {
        InitializeComponent();

        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = initialValue;

        Loaded += (_, _) =>
        {
            InputBox.Focus();
            InputBox.SelectAll();
        };
    }

    private void OnSaveClick(object sender, RoutedEventArgs e) => TrySave();

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnInputBoxKeyDown(object sender, KeyEventArgs e)
    {
        // Marked Handled so this doesn't also bubble up and trigger the
        // Save button's own IsDefault-on-Enter handling a second time,
        // which would call TrySave() twice for one keypress.
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        TrySave();
    }

    private void TrySave()
    {
        if (string.IsNullOrWhiteSpace(InputBox.Text))
        {
            MessageBox.Show(this, "Please enter a name.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
