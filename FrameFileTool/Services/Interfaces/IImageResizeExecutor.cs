using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 執行實際的圖片縮放副作用：讀取來源圖片、依設定調整尺寸並寫出目標檔案。
/// 操作前必須先由 ResizePlanner 產出預覽計畫。
/// 獨立介面，不與 IFileOperationExecutor 合併，以遵守 Interface Segregation Principle。
/// </summary>
public interface IImageResizeExecutor
{
    /// <summary>
    /// 執行預覽清單中標記為 <see cref="OperationAction.Resize"/> 且無錯誤的項目。
    /// </summary>
    OperationResult Execute(
        IEnumerable<OperationPreviewItem> previewItems,
        ResizeOptions options);
}
