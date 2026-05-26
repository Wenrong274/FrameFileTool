using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 依據縮放設定，規劃批次縮放計畫，回傳預覽項目清單，不直接操作圖片檔案。
/// </summary>
public interface IResizePlanner
{
    /// <summary>
    /// 依據 <paramref name="options"/> 規劃每個檔案的縮放計畫。
    /// Pure function：不讀取圖片像素、不執行任何檔案 I/O。
    /// </summary>
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        ResizeOptions options);
}
