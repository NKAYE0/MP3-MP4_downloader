using System.Windows;
using System.Windows.Media;

namespace YtTikDownloader.App.Services;

/// <summary>
/// Applies the user's chosen accent color (Settings > Appearance) to the
/// app-level "AppAccentBrush" resource that Claude-drawn UI -- currently
/// just the download queue's progress bar -- binds to via DynamicResource.
///
/// This deliberately does not try to override WPF's built-in Fluent theme
/// accent (SystemColors.AccentColor and friends): that's tied to the
/// Windows OS accent color setting and, as of .NET 10, isn't documented or
/// supported as something an individual app can override -- native Fluent
/// controls (checkboxes, radio buttons, standard buttons) will keep
/// following Windows' own accent color regardless of what's picked here.
/// </summary>
public static class AccentColorService
{
    public const string ResourceKey = "AppAccentBrush";

    /// <summary>Falls back to this if a stored/typed hex string can't be parsed.</summary>
    public const string DefaultHex = "#FF4FC3F7";

    public static void Apply(string? hex)
    {
        var color = ParseOrDefault(hex);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Application.Current.Resources[ResourceKey] = brush;
    }

    // Fully qualified as System.Windows.Media.Color throughout this file
    // (rather than relying on the "using System.Windows.Media;" above) on
    // purpose: this project also references System.Windows.Forms.ColorDialog
    // elsewhere for the color picker, and System.Drawing.Color/System.Windows.Media.Color
    // share the same short name, so being explicit here means this file
    // keeps compiling correctly no matter what else the project's implicit
    // usings end up including.
    public static System.Windows.Media.Color ParseOrDefault(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                if (ColorConverter.ConvertFromString(hex) is System.Windows.Media.Color parsed) return parsed;
            }
            catch (FormatException)
            {
                // Falls through to the default below -- e.g. a corrupted
                // or hand-edited settings.json.
            }
        }

        return (System.Windows.Media.Color)ColorConverter.ConvertFromString(DefaultHex)!;
    }

    /// <summary>"#AARRGGBB", matching the format ColorConverter itself produces/accepts.</summary>
    public static string ToHex(System.Windows.Media.Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
}
