using System.IO;
using FrameFileTool.Models;

namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 批次改名操作的預覽結果，供 DataTemplate 顯示對應欄位。
/// 除基底的通知轉發外，另維護勾選狀態造成的 selection conflict
/// （已勾選改名目標撞到未勾選來源檔）。
/// </summary>
public sealed class RenamePreviewViewModel : PreviewViewModelBase<OperationPreviewItem>
{
    /// <inheritdoc/>
    public override string Summary
    {
        get
        {
            var includedItems = Items.Where(i => i.IsIncluded).ToList();
            var inclusionConflictItems = GetInclusionConflictItems();
            var renameCount = includedItems.Count(
                i => i.ActionKind == OperationActionKind.Rename && !i.HasError && !inclusionConflictItems.Contains(i));
            var copyCount = includedItems.Count(
                i => i.ActionKind == OperationActionKind.Copy && !i.HasError && !inclusionConflictItems.Contains(i));
            var staticErrorCount = Items.Count(i => i.HasError);
            var errorCount = staticErrorCount + inclusionConflictItems.Count;
            var displayCount = includedItems.Count + staticErrorCount;
            var actionText = copyCount > 0 ? "複製改名" : "改名";
            var actionCount = copyCount > 0 ? copyCount : renameCount;

            return errorCount > 0
                ? $"共 {displayCount} 個項目，預計{actionText} {actionCount} 個，{errorCount} 個錯誤（執行已停用）"
                : $"共 {displayCount} 個項目，預計{actionText} {actionCount} 個";
        }
    }

    /// <inheritdoc/>
    public override bool HasErrors => Items.Any(i => i.HasError) || GetInclusionConflictItems().Count > 0;

    /// <inheritdoc/>
    public override bool HasExecutableItems =>
        Items.Any(i =>
            i.IsIncluded &&
            i.ActionKind is OperationActionKind.Rename or OperationActionKind.Copy &&
            !i.HasError &&
            !GetInclusionConflictItems().Contains(i));

    public RenamePreviewViewModel(IReadOnlyList<OperationPreviewItem> items)
        : base(items)
    {
        RefreshSelectionConflicts();
    }

    /// <inheritdoc/>
    protected override void OnIncludedItemsChanged() => RefreshSelectionConflicts();

    private void RefreshSelectionConflicts()
    {
        var conflictItems = GetInclusionConflictItems();
        foreach (var item in Items)
        {
            item.HasSelectionConflict = conflictItems.Contains(item);
        }
    }

    private HashSet<OperationPreviewItem> GetInclusionConflictItems()
    {
        var sourcePathsHeldInPlace = Items
            .Where(i => !i.IsIncluded || i.ActionKind != OperationActionKind.Rename || i.HasError)
            .Select(i => i.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Items
            .Where(i => i.IsIncluded && i.ActionKind == OperationActionKind.Rename && !i.HasError)
            .Where(i => sourcePathsHeldInPlace.Contains(BuildTargetPath(i)))
            .ToHashSet();
    }

    private static string BuildTargetPath(OperationPreviewItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.TargetPath))
        {
            return item.TargetPath;
        }

        var directory = Path.GetDirectoryName(item.FullPath) ?? string.Empty;
        return Path.Combine(directory, item.TargetName);
    }
}
