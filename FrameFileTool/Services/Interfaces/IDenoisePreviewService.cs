using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 產生降噪局部預覽圖的 service。
/// 對單張來源圖片進行中央裁切，並依指定模式清單分別套用降噪，
/// 輸出可供 WPF 直接顯示的 PNG 位元組陣列。
/// </summary>
public interface IDenoisePreviewService
{
    /// <summary>
    /// 非同步產生各降噪模式的局部預覽。
    /// </summary>
    /// <param name="sourceImagePath">來源圖片的完整路徑。</param>
    /// <param name="modes">要產生預覽的模式清單。</param>
    /// <param name="cancellationToken">取消 token。</param>
    /// <returns>
    /// 每個模式對應的 PNG 位元組陣列；
    /// 若來源無法讀取則 <see cref="DenoisePreviewResult.HasError"/> 為 true。
    /// </returns>
    public Task<DenoisePreviewResult> GeneratePreviewAsync(
        string sourceImagePath,
        IReadOnlyList<DenoiseMode> modes,
        CancellationToken cancellationToken = default);
}
