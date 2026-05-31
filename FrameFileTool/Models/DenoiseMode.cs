namespace FrameFileTool.Models;

/// <summary>
/// 批次縮放的降噪模式。
/// 對應 <see cref="ResizeOptions.DenoiseMode"/>，決定 Magick.NET 的降噪 pipeline。
/// </summary>
public enum DenoiseMode
{
    /// <summary>不套用降噪。</summary>
    Off = 0,

    /// <summary>輕度降噪（ReduceNoise(1)），適合細節豐富的圖片。</summary>
    Detail = 1,

    /// <summary>標準降噪（ReduceNoise(3)），平衡降噪與細節保留。</summary>
    Standard = 2,

    /// <summary>強力降噪（ReduceNoise(3) + WaveletDenoise(25%)），可能使影像偏柔和。</summary>
    Strong = 3,
}
