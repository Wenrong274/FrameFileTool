using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <summary>
/// 依據抽幀間隔 N，規劃哪些檔案應被刪除。
/// 每個子資料夾各自獨立計數，符合「子資料夾視為獨立序列」的規格。
/// Pure planner：不讀寫檔案、不修改任何 shared state。
/// </summary>
public sealed class FrameDeletePlanner : IFrameDeletePlanner
{
    /// <inheritdoc/>
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        int interval)
    {
        // 間隔 N 必須為正整數，否則所有項目標記為錯誤
        if (interval <= 0)
        {
            return files
                .Select((file, index) => new OperationPreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    ActionKind = OperationActionKind.Error,
                    TargetName = string.Empty,
                    Status = "間隔 N 必須大於 0",
                    HasError = true,
                })
                .ToList();
        }

        // 每個資料夾獨立維護計數器，確保子資料夾從第 1 張重新計算
        var folderCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        return files
            .Select((file, index) =>
            {
                folderCounters.TryGetValue(file.DirectoryPath, out var folderIndex);
                folderIndex++;
                folderCounters[file.DirectoryPath] = folderIndex;

                // 每 interval 張刪除第 interval 張（folderIndex 為資料夾內序號）
                var shouldDelete = folderIndex % interval == 0;
                return new OperationPreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    ActionKind = shouldDelete ? OperationActionKind.Delete : OperationActionKind.Keep,
                    TargetName = shouldDelete ? "移到回收桶" : string.Empty,
                    Status = shouldDelete
                        ? $"資料夾內第 {folderIndex} 張符合每 {interval} 張刪除 1 張"
                        : "不處理",
                    HasError = false,
                };
            })
            .ToList();
    }
}
