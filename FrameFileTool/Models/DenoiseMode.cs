namespace FrameFileTool.Models;

/// <summary>
/// 批次降噪的降噪模式。
/// 對應 <see cref="DenoiseOptions.Mode"/>，決定 Magick.NET 的降噪 pipeline。
/// </summary>
public enum DenoiseMode
{
    /// <summary>
    /// 不套用降噪。批次降噪工具的模式選單不提供此值，
    /// 保留給降噪預覽服務產生「原圖對照」使用。
    /// </summary>
    Off = 0,

    /// <summary>輕度降噪（WaveletDenoise 10%），適合細節豐富的圖片。</summary>
    Detail = 1,

    /// <summary>標準降噪（ReduceNoise(3)），平衡降噪與細節保留。</summary>
    Standard = 2,

    /// <summary>強力降噪（ReduceNoise(3) + WaveletDenoise 25%），可能使影像偏柔和。</summary>
    Strong = 3,
}
