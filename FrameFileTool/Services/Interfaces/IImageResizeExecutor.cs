using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 執行實際的圖片縮放副作用，讀取來源圖片並寫出目標檔案。
/// 操作前必須先由 ResizePlanner 產出預覽計畫。
/// 獨立介面，不與 IFileOperationExecutor 合併，以遵守 Interface Segregation Principle。
/// </summary>
public interface IImageResizeExecutor
{
    /// <summary>
    /// 同步版本（保留給測試或非 UI 情境使用）。
    /// 執行預覽清單中標記為 <see cref="OperationAction.Resize"/> 且無錯誤的項目。
    /// </summary>
    public OperationResult Execute(
        IEnumerable<OperationPreviewItem> previewItems,
        ResizeOptions options);

    /// <summary>
    /// 非同步版本，供 UI 呼叫以避免畫面凍結。
    /// 透過 <paramref name="progress"/> 逐檔回報進度，支援 <paramref name="cancellationToken"/> 中途取消。
    /// </summary>
    public Task<OperationResult> ExecuteAsync(
        IEnumerable<OperationPreviewItem> previewItems,
        ResizeOptions options,
        IProgress<ResizeProgressReport>? progress = null,
        CancellationToken cancellationToken = default);
}
