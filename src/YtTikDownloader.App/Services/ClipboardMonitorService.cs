using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace YtTikDownloader.App.Services;

/// <summary>
/// Watches the Windows clipboard for changes using the standard
/// AddClipboardFormatListener / WM_CLIPBOARDUPDATE mechanism, and raises
/// TextCopied whenever the new clipboard content is plain text. The
/// caller (MainViewModel) decides whether the text looks like a
/// supported URL -- this class only knows about the clipboard itself.
/// </summary>
public sealed class ClipboardMonitorService : IDisposable
{
    private const int WM_CLIPBOARDUPDATE = 0x031D;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    private readonly HwndSource _hwndSource;
    private string? _lastSeenText;
    private bool _disposed;

    public event Action<string>? TextCopied;

    public ClipboardMonitorService(Window window)
    {
        var helper = new WindowInteropHelper(window);
        // The window must already have a handle; MainWindow calls this
        // from its Loaded event so this is guaranteed.
        _hwndSource = HwndSource.FromHwnd(helper.Handle)
            ?? throw new InvalidOperationException("Window handle not available yet; start clipboard monitoring after the window is Loaded.");

        _hwndSource.AddHook(WndProc);
        AddClipboardFormatListener(_hwndSource.Handle);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            TryReadClipboardText();
        }
        return IntPtr.Zero;
    }

    private void TryReadClipboardText()
    {
        // Clipboard access can throw if another process briefly holds the
        // clipboard open; that's a normal, harmless race, not an error.
        try
        {
            if (!Clipboard.ContainsText()) return;
            var text = Clipboard.GetText()?.Trim();
            if (string.IsNullOrEmpty(text) || text == _lastSeenText) return;

            _lastSeenText = text;
            TextCopied?.Invoke(text);
        }
        catch (COMException)
        {
        }
        catch (ExternalException)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        RemoveClipboardFormatListener(_hwndSource.Handle);
        _hwndSource.RemoveHook(WndProc);
    }
}
