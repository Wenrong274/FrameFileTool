using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 依據命名樣板與編號來源，規劃批次改名計畫並偵測衝突，不直接操作檔案。
/// </summary>
public interface IRenamePlanner
{
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        RenameOptions options,
        IReadOnlySet<string>? existingPaths = null);

    /// <summary>
    /// 依相同的逐資料夾計數與命名公式，計算每個檔案的目標完整路徑，
    /// 供呼叫端用於執行前的目標檔案存在性查詢。
    /// 沿用原編號但檔名無編號的檔案不會產生目標路徑。
    /// </summary>
    public IEnumerable<string> ProjectTargetPaths(
        IReadOnlyList<FileItem> files,
        RenameOptions options);
}
