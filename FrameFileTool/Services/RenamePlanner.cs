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
/// 目前會查詢目標檔案是否已存在，用於避免覆蓋計畫外檔案；
/// 其餘規劃邏輯不修改 shared state，也不執行任何改名副作用。
/// </summary>
public sealed class RenamePlanner : IRenamePlanner
{
    /// <inheritdoc/>
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        string prefix,
        int startIndex,
        int padding,
        IReadOnlySet<string>? existingPaths = null,
        RenameOutputMode outputMode = RenameOutputMode.RenameInPlace,
        string targetFolderPath = "")
    {
        var plannedItems = new List<OperationPreviewItem>(files.Count);

        var chooseTargetFolderOnExecute = outputMode == RenameOutputMode.CopyToTargetFolder &&
            string.IsNullOrWhiteSpace(targetFolderPath);

        if (outputMode == RenameOutputMode.CopyToTargetFolder &&
            !chooseTargetFolderOnExecute &&
            !PathSafetyValidator.IsSafeTargetDirectoryPath(targetFolderPath))
        {
            return files
                .Select((file, index) => new OperationPreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    ActionKind = OperationActionKind.Error,
                    TargetName = string.Empty,
                    Status = "目標資料夾路徑不安全",
                    HasError = true,
                })
                .ToList();
        }

        // 追蹤本次計畫已使用的目標路徑，用來偵測計畫內重複
        var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 來源路徑集合：若目標路徑在此集合內，代表是「改名鏈」的一環，不算衝突
        var sourcePaths = files
            .Select(f => f.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var knownExistingPaths = existingPaths ?? sourcePaths;

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

            if (!PathSafetyValidator.IsSafeFileName(targetName))
            {
                plannedItems.Add(new OperationPreviewItem
                {
                    Index = i + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    ActionKind = OperationActionKind.Error,
                    TargetName = targetName,
                    Status = "目標檔名包含路徑或不允許的字元",
                    HasError = true,
                });
                continue;
            }

            var targetPath = outputMode == RenameOutputMode.CopyToTargetFolder && !chooseTargetFolderOnExecute
                ? Path.Combine(targetFolderPath, targetName)
                : Path.Combine(file.DirectoryPath, targetName);

            // 衝突偵測
            var hasDuplicateTarget = !chooseTargetFolderOnExecute && !targetPaths.Add(targetPath);
            var targetExistsOutsidePlan = knownExistingPaths.Contains(targetPath) &&
                (outputMode == RenameOutputMode.CopyToTargetFolder && !chooseTargetFolderOnExecute || !sourcePaths.Contains(targetPath));
            var sameName = outputMode == RenameOutputMode.RenameInPlace &&
                string.Equals(file.FullPath, targetPath, StringComparison.OrdinalIgnoreCase);
            var hasError = hasDuplicateTarget || targetExistsOutsidePlan;

            var status = DetermineStatus(hasDuplicateTarget, targetExistsOutsidePlan, sameName, outputMode);
            var actionKind = outputMode == RenameOutputMode.CopyToTargetFolder
                ? OperationActionKind.Copy
                : sameName ? OperationActionKind.Keep : OperationActionKind.Rename;

            plannedItems.Add(new OperationPreviewItem
            {
                Index = i + 1,
                FullPath = file.FullPath,
                OriginalName = file.Name,
                ActionKind = actionKind,
                TargetName = outputMode == RenameOutputMode.CopyToTargetFolder
                    ? chooseTargetFolderOnExecute ? "執行時選擇資料夾" : targetPath
                    : targetName,
                TargetPath = chooseTargetFolderOnExecute ? string.Empty : targetPath,
                Status = status,
                HasError = hasError,
            });
        }

        return plannedItems;
    }

    /// <inheritdoc/>
    public IEnumerable<string> ProjectTargetPaths(
        IReadOnlyList<FileItem> files,
        string prefix,
        int startIndex,
        int padding,
        string targetFolderPath)
    {
        var folderCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return files.Select(file =>
        {
            folderCounters.TryGetValue(file.DirectoryPath, out var folderIndex);
            folderCounters[file.DirectoryPath] = folderIndex + 1;
            var number = startIndex + folderIndex;
            var numberText = padding > 0
                ? number.ToString().PadLeft(padding, '0')
                : number.ToString();
            return Path.Combine(targetFolderPath, $"{prefix}{numberText}{file.Extension}");
        });
    }

    /// <summary>依衝突類型決定狀態說明文字。</summary>
    private static string DetermineStatus(
        bool hasDuplicateTarget,
        bool targetExistsOutsidePlan,
        bool sameName,
        RenameOutputMode outputMode)
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

        return outputMode == RenameOutputMode.CopyToTargetFolder
            ? "可複製並改名"
            : "可改名";
    }
}
