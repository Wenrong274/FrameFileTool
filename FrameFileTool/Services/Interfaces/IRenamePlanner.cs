using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 依據前綴、起始編號與補零位數，規劃批次改名計畫並偵測衝突，不直接操作檔案。
/// </summary>
public interface IRenamePlanner
{
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        string prefix,
        int startIndex,
        int padding,
        IReadOnlySet<string>? existingPaths = null,
        RenameOutputMode outputMode = RenameOutputMode.RenameInPlace,
        string targetFolderPath = "");

    /// <summary>
    /// 依相同的逐資料夾計數與命名公式，計算每個檔案的目標完整路徑，
    /// 供呼叫端用於執行前的目標檔案存在性查詢。
    /// </summary>
    public IEnumerable<string> ProjectTargetPaths(
        IReadOnlyList<FileItem> files,
        string prefix,
        int startIndex,
        int padding,
        string targetFolderPath);
}
