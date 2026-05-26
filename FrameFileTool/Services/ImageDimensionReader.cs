using FrameFileTool.Services.Interfaces;
using ImageMagick;

namespace FrameFileTool.Services;

/// <summary>
/// 使用 Magick.NET 的 <see cref="MagickImageInfo"/> 讀取圖片尺寸。
/// <see cref="MagickImageInfo"/> 只解析檔案標頭，不載入像素資料，
/// 批次讀取幾百張圖仍可在毫秒級完成。
/// </summary>
public sealed class ImageDimensionReader : IImageDimensionReader
{
    /// <inheritdoc/>
    public (int Width, int Height) Read(string filePath)
    {
        var info = new MagickImageInfo(filePath);
        return ((int)info.Width, (int)info.Height);
    }
}
