using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 執行實際的檔案系統副作用：移到回收桶、批次改名。
/// </summary>
public interface IFileOperationExecutor
{
    public OperationResult DeleteToRecycleBin(IEnumerable<OperationPreviewItem> previewItems);
    public OperationResult RenameFiles(IEnumerable<OperationPreviewItem> previewItems);
}
