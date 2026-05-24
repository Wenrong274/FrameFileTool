using FrameFileTool.Models;

namespace FrameFileTool.Services;

public sealed class RenamePlanner
{
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        string prefix,
        int startIndex,
        int padding)
    {
        var plannedItems = new List<OperationPreviewItem>();
        var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourcePaths = files
            .Select(file => file.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var number = startIndex + i;
            var numberText = padding > 0
                ? number.ToString().PadLeft(padding, '0')
                : number.ToString();
            var targetName = $"{prefix}{numberText}{file.Extension}";
            var targetPath = Path.Combine(file.DirectoryPath, targetName);

            var hasDuplicateTarget = !targetPaths.Add(targetPath);
            var targetExistsOutsidePlan = File.Exists(targetPath) &&
                                          !sourcePaths.Contains(targetPath);
            var sameName = string.Equals(file.FullPath, targetPath, StringComparison.OrdinalIgnoreCase);
            var hasError = hasDuplicateTarget || targetExistsOutsidePlan;

            var status = "可改名";
            if (hasDuplicateTarget)
            {
                status = "目標檔名重複";
            }
            else if (targetExistsOutsidePlan)
            {
                status = "目標檔案已存在";
            }
            else if (sameName)
            {
                status = "檔名相同，不處理";
            }

            plannedItems.Add(new OperationPreviewItem
            {
                Index = i + 1,
                FullPath = file.FullPath,
                OriginalName = file.Name,
                Action = sameName ? "保留" : "改名",
                TargetName = targetName,
                Status = status,
                HasError = hasError
            });
        }

        return plannedItems;
    }
}
