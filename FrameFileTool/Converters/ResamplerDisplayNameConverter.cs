using System.Globalization;
using System.Windows.Data;
using FrameFileTool.Models;

namespace FrameFileTool.Converters;

/// <summary>
/// 將 <see cref="ResamplerType"/> 轉換為使用者可讀的中文顯示名稱，供 ComboBox 項目顯示。
/// </summary>
[ValueConversion(typeof(ResamplerType), typeof(string))]
public sealed class ResamplerDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is ResamplerType resampler
            ? ToDisplayName(resampler)
            : value?.ToString() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException("ResamplerDisplayNameConverter 僅支援單向繫結");

    private static string ToDisplayName(ResamplerType resampler) => resampler switch
    {
        ResamplerType.Bicubic           => "一般用途（Bicubic）",
        ResamplerType.Lanczos3          => "高品質縮小（Lanczos3）",
        ResamplerType.CatmullRom        => "高品質放大（CatmullRom）",
        ResamplerType.NearestNeighbor   => "像素精準（NearestNeighbor）",
        ResamplerType.MitchellNetravali => "銳利優先（MitchellNetravali）",
        _                               => resampler.ToString(),
    };
}
