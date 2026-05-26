using System.Globalization;
using System.Windows.Data;

namespace FrameFileTool.Converters;

/// <summary>
/// 將 enum 值與 ConverterParameter 比較，供 RadioButton 的 IsChecked 雙向繫結使用。
/// </summary>
[ValueConversion(typeof(Enum), typeof(bool))]
public sealed class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is not null)
        {
            return Enum.Parse(targetType, parameter.ToString()!);
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}
