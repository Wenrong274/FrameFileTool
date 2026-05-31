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
}
