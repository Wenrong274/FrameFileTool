using System.Globalization;
using System.Windows.Data;
using FrameFileTool.Models;

namespace FrameFileTool.Converters;

/// <summary>
/// 將 <see cref="DenoiseMode"/> 轉換為使用者可讀的中文顯示名稱，供 ComboBox 項目顯示。
/// </summary>
[ValueConversion(typeof(DenoiseMode), typeof(string))]
public sealed class DenoiseModeDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DenoiseMode mode ? ToDisplayName(mode) : value?.ToString() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("DenoiseModeDisplayNameConverter 僅支援單向繫結");

    private static string ToDisplayName(DenoiseMode mode) => mode switch
    {
        DenoiseMode.Off => "關閉",
        DenoiseMode.Detail => "保留細節",
        DenoiseMode.Standard => "標準",
        DenoiseMode.Strong => "強力",
        _ => mode.ToString(),
    };
}
