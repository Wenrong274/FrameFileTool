using FrameFileTool.Models;
using ImageMagick;

namespace FrameFileTool.Services;

/// <summary>
/// 降噪演算法的單一實作點，由 <see cref="DenoisePreviewService"/>（局部預覽）
/// 與 <see cref="DenoiseExecutor"/>（批次執行）共用，避免兩處 pipeline 分岔。
/// </summary>
internal static class DenoiseImageProcessor
{
    /// <summary>依指定模式對影像就地套用降噪。<see cref="DenoiseMode.Off"/> 不做任何處理。</summary>
    internal static void ApplyDenoise(MagickImage image, DenoiseMode mode)
    {
        switch (mode)
        {
            case DenoiseMode.Detail:
                image.WaveletDenoise(new Percentage(10));
                break;
            case DenoiseMode.Standard:
                image.ReduceNoise(3);
                break;
            case DenoiseMode.Strong:
                image.ReduceNoise(3);
                image.WaveletDenoise(new Percentage(25));
                break;
        }
    }
}
