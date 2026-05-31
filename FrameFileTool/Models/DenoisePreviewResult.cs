namespace FrameFileTool.Models;

/// <summary>
/// <see cref="Services.Interfaces.IDenoisePreviewService"/> 的輸出結果。
/// 每個 <see cref="DenoiseMode"/> 對應一張裁切後的 PNG 位元組陣列，可供 WPF 直接顯示。
/// </summary>
public sealed record DenoisePreviewResult(
    IReadOnlyDictionary<DenoiseMode, byte[]> PreviewsByMode,
    string? ErrorMessage = null)
{
    public bool HasError => ErrorMessage is not null;
}
