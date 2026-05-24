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

        return files
            .Select((file, index) =>
            {
                var shouldDelete = (index + 1) % interval == 0;
                return new OperationPreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    Action = shouldDelete ? "刪除" : "保留",
                    TargetName = shouldDelete ? "移到回收桶" : string.Empty,
                    Status = shouldDelete ? $"第 {index + 1} 張符合每 {interval} 張刪除 1 張" : "不處理",
                    HasError = false
                };
            })
            .ToList();
    }
}
