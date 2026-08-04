using System.IO;
using System.Text.RegularExpressions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <summary>
/// 依據命名樣板與編號來源，規劃批次改名計畫。
/// 重新編號時每個子資料夾各自從 0 起算獨立計數；沿用原編號時改用原檔名最後一組連續數字。
/// 並偵測下列兩種衝突：
/// <list type="bullet">
///   <item>計畫內重複目標檔名（兩個檔案改名到同一個名稱）</item>
///   <item>計畫外已存在的目標檔案（會覆蓋非計畫中的檔案）</item>
/// </list>
/// 目前會查詢目標檔案是否已存在，用於避免覆蓋計畫外檔案；
/// 其餘規劃邏輯不修改 shared state，也不執行任何改名副作用。
/// </summary>
public sealed partial class RenamePlanner : IRenamePlanner
{
    /// <summary>命名樣板的編號槽位：<c>[</c> 加一個以上 <c>#</c> 再加 <c>]</c>。</summary>
    [GeneratedRegex(@"\[#+\]")]
    private static partial Regex NumberTokenPattern { get; }

    /// <summary>檔名中最後一組連續數字（後方不再有任何數字）。</summary>
    [GeneratedRegex(@"\d+(?!.*\d)")]
    private static partial Regex LastNumberPattern { get; }

    /// <inheritdoc/>
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        RenameOptions options,
        IReadOnlySet<string>? existingPaths = null)
    {
        if (!NumberTokenPattern.IsMatch(options.Template))
        {
            return BuildAllErrorItems(files, "命名樣板缺少 [###] 編號欄位");
        }

        var chooseTargetFolderOnExecute = options.OutputMode == RenameOutputMode.CopyToTargetFolder &&
            string.IsNullOrWhiteSpace(options.TargetFolderPath);

        if (options.OutputMode == RenameOutputMode.CopyToTargetFolder &&
            !chooseTargetFolderOnExecute &&
            !PathSafetyValidator.IsSafeTargetDirectoryPath(options.TargetFolderPath))
        {
            return BuildAllErrorItems(files, "目標資料夾路徑不安全");
        }

        var plannedItems = new List<OperationPreviewItem>(files.Count);

        // 追蹤本次計畫已使用的目標路徑，用來偵測計畫內重複
        var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 來源路徑集合：若目標路徑在此集合內，代表是「改名鏈」的一環，不算衝突
        var sourcePaths = files
            .Select(f => f.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var knownExistingPaths = existingPaths ?? sourcePaths;

        // 每個資料夾獨立計數，一律從 0 開始
        var folderCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];

            folderCounters.TryGetValue(file.DirectoryPath, out var folderIndex);
            folderCounters[file.DirectoryPath] = folderIndex + 1;

            var targetName = BuildTargetName(file, options, folderIndex);

            // 沿用原編號但檔名找不到數字：略過，不視為錯誤，避免阻擋整批執行
            if (targetName is null)
            {
                plannedItems.Add(new OperationPreviewItem
                {
                    Index = i + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    ActionKind = OperationActionKind.Keep,
                    TargetName = file.Name,
                    Status = "無編號，不處理",
                    HasError = false,
                });
                continue;
            }

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

            var targetPath = options.OutputMode == RenameOutputMode.CopyToTargetFolder && !chooseTargetFolderOnExecute
                ? Path.Combine(options.TargetFolderPath, targetName)
                : Path.Combine(file.DirectoryPath, targetName);

            // 衝突偵測
            var hasDuplicateTarget = !chooseTargetFolderOnExecute && !targetPaths.Add(targetPath);
            var targetExistsOutsidePlan = knownExistingPaths.Contains(targetPath) &&
                (options.OutputMode == RenameOutputMode.CopyToTargetFolder && !chooseTargetFolderOnExecute || !sourcePaths.Contains(targetPath));
            var sameName = options.OutputMode == RenameOutputMode.RenameInPlace &&
                string.Equals(file.FullPath, targetPath, StringComparison.OrdinalIgnoreCase);
            var hasError = hasDuplicateTarget || targetExistsOutsidePlan;

            var status = DetermineStatus(hasDuplicateTarget, targetExistsOutsidePlan, sameName, options.OutputMode);
            var actionKind = options.OutputMode == RenameOutputMode.CopyToTargetFolder
                ? OperationActionKind.Copy
                : sameName ? OperationActionKind.Keep : OperationActionKind.Rename;

            plannedItems.Add(new OperationPreviewItem
            {
                Index = i + 1,
                FullPath = file.FullPath,
                OriginalName = file.Name,
                ActionKind = actionKind,
                TargetName = options.OutputMode == RenameOutputMode.CopyToTargetFolder
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
        RenameOptions options)
    {
        if (!NumberTokenPattern.IsMatch(options.Template))
        {
            return [];
        }

        var folderCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        return files
            .Select(file =>
            {
                folderCounters.TryGetValue(file.DirectoryPath, out var folderIndex);
                folderCounters[file.DirectoryPath] = folderIndex + 1;
                return BuildTargetName(file, options, folderIndex);
            })
            .Where(name => name is not null)
            .Select(name => Path.Combine(options.TargetFolderPath, name!))
            .ToList();
    }

    /// <summary>
    /// 依編號來源計算目標檔名。
    /// 沿用原編號但檔名（不含副檔名）找不到任何數字時回傳 <see langword="null"/>。
    /// </summary>
    private static string? BuildTargetName(FileItem file, RenameOptions options, int folderIndex)
    {
        if (!options.UseOriginalNumber)
        {
            return ApplyTemplate(options.Template, padding => folderIndex.ToString().PadLeft(padding, '0')) +
                file.Extension;
        }

        var split = SplitTrailingNumber(file.Name);
        if (split is null)
        {
            return null;
        }

        // 原編號原封不動代入，樣板的補零位數不生效；數字後方的尾綴接在展開後的樣板之後。
        var (originalNumber, suffix) = split.Value;
        return ApplyTemplate(options.Template, _ => originalNumber) + suffix + file.Extension;
    }

    /// <summary>將樣板的每個編號槽位替換為 <paramref name="render"/> 的輸出，其餘字元一律字面保留。</summary>
    private static string ApplyTemplate(string template, Func<int, string> render) =>
        NumberTokenPattern.Replace(template, match => render(match.Length - 2));

    /// <summary>
    /// 取出檔名（先剝除副檔名，避免 <c>.mp3</c> 這類含數字的副檔名被誤判）
    /// 最後一組連續數字，以及該數字後方的尾綴。
    /// </summary>
    private static (string Number, string Suffix)? SplitTrailingNumber(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        var match = LastNumberPattern.Match(stem);
        return match.Success
            ? (match.Value, stem[(match.Index + match.Length)..])
            : null;
    }

    /// <summary>將所有檔案標記為同一種規劃層錯誤，例如樣板或目標資料夾設定不正確。</summary>
    private static List<OperationPreviewItem> BuildAllErrorItems(
        IReadOnlyList<FileItem> files,
        string status) =>
        files
            .Select((file, index) => new OperationPreviewItem
            {
                Index = index + 1,
                FullPath = file.FullPath,
                OriginalName = file.Name,
                ActionKind = OperationActionKind.Error,
                TargetName = string.Empty,
                Status = status,
                HasError = true,
            })
            .ToList();

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
