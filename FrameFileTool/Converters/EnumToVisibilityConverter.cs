using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FrameFileTool.Converters;

/// <summary>
/// 將 enum 值與 ConverterParameter 比較，相同時回傳 Visible，否則回傳 Collapsed。
/// 供依模式切換顯示的 StackPanel 使用。
/// </summary>
[ValueConversion(typeof(Enum), typeof(Visibility))]
public sealed class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString()
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("EnumToVisibilityConverter 僅支援單向繫結");
}
