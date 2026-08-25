using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace YtTikDownloader.App.Converters;

/// <summary>
/// Turns a 0-100 progress percentage into a star-sized GridLength, used to
/// build the download queue's progress bar out of two plain Grid columns
/// (fill + remainder) instead of a native ProgressBar.
///
/// This exists because the built-in WPF Fluent-theme ProgressBar was
/// rendering its indicator wider than its own container -- visually
/// spilling out past the edges of the queue item card -- regardless of the
/// bound Value. Driving column widths through Grid star-sizing sidesteps
/// that control's template entirely: a Grid can only ever divide up the
/// width it actually has, so the fill segment is structurally incapable of
/// rendering past the bar's bounds.
///
/// Pass ConverterParameter="Remainder" for the second (empty/track) column
/// so its width is always exactly 100 minus the fill column's percentage;
/// omit it (or pass anything else) for the fill column itself.
/// </summary>
public sealed class PercentToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percent = value is double d ? d : 0d;
        percent = Math.Clamp(percent, 0, 100);

        var isRemainder = string.Equals(parameter as string, "Remainder", StringComparison.OrdinalIgnoreCase);
        var stars = isRemainder ? 100 - percent : percent;

        // A zero-star column is valid, but keep a hair of width so the
        // Grid never has to divide by a total of zero stars (both columns
        // at exactly 0% progress).
        return new GridLength(Math.Max(stars, 0.01), GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
