using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace YtTikDownloader.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            // If the window itself fails to construct (e.g. a XAML load
            // error), there's no window for the app to keep running
            // behind, so show the real error and exit cleanly instead of
            // leaving an invisible process running.
            ShowError(ex);
            Shutdown(-1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowError(e.Exception);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex) ShowError(ex);
    }

    /// <summary>
    /// WPF/reflection failures (e.g. a XAML load error) surface as a
    /// generic "Exception has been thrown by the target of an invocation"
    /// wrapper -- the actually useful message is in InnerException, or
    /// further down the chain. This walks the whole chain so the real
    /// cause is always visible instead of just the wrapper text.
    /// </summary>
    private static void ShowError(Exception ex)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Something went wrong. Full details below -- please copy this and share it:");
        sb.AppendLine();

        var current = ex;
        var depth = 0;
        while (current is not null && depth < 6)
        {
            sb.AppendLine($"[{current.GetType().FullName}] {current.Message}");
            current = current.InnerException;
            depth++;
        }

        sb.AppendLine();
        sb.AppendLine("Stack trace:");
        sb.AppendLine(ex.StackTrace ?? "(none)");

        MessageBox.Show(sb.ToString(), "YtTikDownloader - Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
