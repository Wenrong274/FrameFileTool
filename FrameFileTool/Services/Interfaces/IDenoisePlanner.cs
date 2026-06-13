using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 依據降噪設定，規劃批次降噪計畫，回傳預覽項目清單，不直接操作圖片檔案。
/// </summary>
public interface IDenoisePlanner
{
    /// <summary>
    /// 依據 <paramref name="options"/> 規劃每個檔案的降噪計畫。
    /// Pure function：不讀取圖片像素、不執行任何檔案 I/O。
    /// 批次降噪固定覆寫原檔，目標路徑即來源路徑。
    /// </summary>
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        DenoiseOptions options);
}
