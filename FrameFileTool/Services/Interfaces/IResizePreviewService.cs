using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 建立縮放預覽所需資料，包含讀取圖片尺寸與檢查輸出檔是否已存在。
/// </summary>
public interface IResizePreviewService
{
    /// <summary>依檔案清單與縮放選項建立縮放預覽項目。</summary>
    public Task<IReadOnlyList<ResizePreviewItem>> BuildPreviewAsync(
        IReadOnlyList<FileItem> files,
        ResizeOptions options,
        CancellationToken cancellationToken = default);
}
