using FrameFileTool.Models;

namespace FrameFileTool.Services;

public sealed class FrameDeletePlanner
{
    public IReadOnlyList<OperationPreviewItem> Plan(IReadOnlyList<FileItem> files, int interval)
    {
        if (interval <= 0)
        {
            return files
                .Select((file, index) => new OperationPreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    Action = "錯誤",
                    TargetName = string.Empty,
                    Status = "間隔 N 必須大於 0",
                    HasError = true
                })
                .ToList();
        }

        var folderCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        return files
            .Select((file, index) =>
            {
                folderCounters.TryGetValue(file.DirectoryPath, out var folderIndex);
                folderIndex++;
                folderCounters[file.DirectoryPath] = folderIndex;

                var shouldDelete = folderIndex % interval == 0;
                return new OperationPreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    Action = shouldDelete ? "刪除" : "保留",
                    TargetName = shouldDelete ? "移到回收桶" : string.Empty,
                    Status = shouldDelete ? $"資料夾內第 {folderIndex} 張符合每 {interval} 張刪除 1 張" : "不處理",
                    HasError = false
                };
            })
            .ToList();
    }
}
