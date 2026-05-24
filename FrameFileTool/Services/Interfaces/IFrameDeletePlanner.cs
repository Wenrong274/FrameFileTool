using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 依據抽幀間隔，規劃哪些檔案應被刪除，回傳預覽計畫，不直接操作檔案。
/// </summary>
public interface IFrameDeletePlanner
{
    IReadOnlyList<OperationPreviewItem> Plan(IReadOnlyList<FileItem> files, int interval);
}
