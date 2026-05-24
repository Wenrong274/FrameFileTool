using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <summary>
/// 依據前綴、起始編號與補零位數，規劃批次改名計畫。
/// 每個子資料夾各自獨立計數，並偵測下列兩種衝突：
/// <list type="bullet">
///   <item>計畫內重複目標檔名（兩個檔案改名到同一個名稱）</item>
///   <item>計畫外已存在的目標檔案（會覆蓋非計畫中的檔案）</item>
/// </list>
/// Pure planner：不讀寫檔案、不修改任何 shared state。
/// </summary>
public sealed class RenamePlanner : IRenamePlanner
{
    /// <inheritdoc/>
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        string prefix,
        int startIndex,
        int padding)
    {
        var plannedItems = new List<OperationPreviewItem>(files.Count);

        // 追蹤本次計畫已使用的目標路徑，用來偵測計畫內重複
        var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 來源路徑集合：若目標路徑在此集合內，代表是「改名鏈」的一環，不算衝突
        var sourcePaths = files
            .Select(f => f.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 每個資料夾獨立計數，從 startIndex 開始
        var folderCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];

            folderCounters.TryGetValue(file.DirectoryPath, out var folderIndex);
            folderCounters[file.DirectoryPath] = folderIndex + 1;

            // 計算目標檔名
            var number = startIndex + folderIndex;
            var numberText = padding > 0
                ? number.ToString().PadLeft(padding, '0')
                : number.ToString();
            var targetName = $"{prefix}{numberText}{file.Extension}";
            var targetPath = Path.Combine(file.DirectoryPath, targetName);

            // 衝突偵測
            var hasDuplicateTarget = !targetPaths.Add(targetPath);
            var targetExistsOutsidePlan = File.Exists(targetPath) && !sourcePaths.Contains(targetPath);
            var sameName = string.Equals(file.FullPath, targetPath, StringComparison.OrdinalIgnoreCase);
            var hasError = hasDuplicateTarget || targetExistsOutsidePlan;

            var status = DetermineStatus(hasDuplicateTarget, targetExistsOutsidePlan, sameName);

            plannedItems.Add(new OperationPreviewItem
            {
                Index = i + 1,
                FullPath = file.FullPath,
                OriginalName = file.Name,
                Action = sameName ? OperationAction.Keep : OperationAction.Rename,
                TargetName = targetName,
                Status = status,
                HasError = hasError,
            });
        }

        return plannedItems;
    }

    /// <summary>依衝突類型決定狀態說明文字。</summary>
    private static string DetermineStatus(
        bool hasDuplicateTarget,
        bool targetExistsOutsidePlan,
        bool sameName)
    {
        if (hasDuplicateTarget)
        {
            return "目標檔名重複";
        }

        if (targetExistsOutsidePlan)
        {
            return "目標檔案已存在";
        }

        if (sameName)
        {
            return "檔名相同，不處理";
        }

        return "可改名";
    }
}
