using System.Globalization;
using System.Windows.Data;

namespace FrameFileTool.Converters;

/// <summary>
/// 將 Slider 產生的 double 倍率整理成短文字，避免浮點誤差造成過長小數。
/// </summary>
public sealed class ScaleFactorTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is double factor
            ? factor.ToString("0.##", culture)
            : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text &&
            double.TryParse(text, NumberStyles.Float, culture, out var factor))
        {
            return factor;
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}
