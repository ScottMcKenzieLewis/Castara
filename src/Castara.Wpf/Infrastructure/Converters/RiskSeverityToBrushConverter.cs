using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Castara.Domain.Estimation.Models.Outputs;

namespace Castara.Wpf.Infrastructure.Converters;

public sealed class RiskSeverityToBrushConverter : IValueConverter
{
    private static readonly Brush Low = Make("#35C759");
    private static readonly Brush Medium = Make("#FFCC00");
    private static readonly Brush High = Make("#FF3B30");
    private static readonly Brush Default = Brushes.Gray;

    private static Brush Make(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        if (brush.CanFreeze) brush.Freeze();
        return brush;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RiskSeverity severity) return Default;

        return severity switch
        {
            RiskSeverity.Low => Low,
            RiskSeverity.Medium => Medium,
            RiskSeverity.High => High,
            _ => Default
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}