using System.ComponentModel;
using System.IO;
using FrameFileTool.Models;

namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 批次改名操作的預覽結果，供 DataTemplate 顯示對應欄位。
/// </summary>
public sealed class RenamePreviewViewModel : IPreviewViewModel
{
    /// <summary>預覽項目清單，繫結到批次改名專用的 DataGrid。</summary>
    public IReadOnlyList<OperationPreviewItem> Items { get; }

    /// <inheritdoc/>
    public string Summary
    {
        get
        {
            var includedItems = Items.Where(i => i.IsIncluded).ToList();
            var inclusionConflictItems = GetInclusionConflictItems();
            var renameCount = includedItems.Count(
                i => i.Action == OperationAction.Rename && !i.HasError && !inclusionConflictItems.Contains(i));
            var staticErrorCount = Items.Count(i => i.HasError);
            var errorCount = staticErrorCount + inclusionConflictItems.Count;
            var displayCount = includedItems.Count + staticErrorCount;

            return errorCount > 0
                ? $"共 {displayCount} 個項目，預計改名 {renameCount} 個，{errorCount} 個錯誤（執行已停用）"
                : $"共 {displayCount} 個項目，預計改名 {renameCount} 個";
        }
    }

    /// <inheritdoc/>
    public bool HasErrors => Items.Any(i => i.HasError) || GetInclusionConflictItems().Count > 0;

    /// <inheritdoc/>
    public bool HasExecutableItems =>
        Items.Any(i =>
            i.IsIncluded &&
            i.Action == OperationAction.Rename &&
            !i.HasError &&
            !GetInclusionConflictItems().Contains(i));

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    public RenamePreviewViewModel(IReadOnlyList<OperationPreviewItem> items)
    {
        Items = items;

        foreach (var item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }

        RefreshSelectionConflicts();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OperationPreviewItem.IsIncluded))
        {
            return;
        }

        RefreshSelectionConflicts();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasExecutableItems)));
    }

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
            .Where(i => !i.IsIncluded || i.Action != OperationAction.Rename || i.HasError)
            .Select(i => i.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Items
            .Where(i => i.IsIncluded && i.Action == OperationAction.Rename && !i.HasError)
            .Where(i => sourcePathsHeldInPlace.Contains(BuildTargetPath(i)))
            .ToHashSet();
    }

    private static string BuildTargetPath(OperationPreviewItem item)
    {
        var directory = Path.GetDirectoryName(item.FullPath) ?? string.Empty;
        return Path.Combine(directory, item.TargetName);
    }
}
