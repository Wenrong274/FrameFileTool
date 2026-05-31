using System.ComponentModel;
using FrameFileTool.Models;

namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 抽幀刪除操作的預覽結果，供 DataTemplate 顯示對應欄位。
/// </summary>
public sealed class FrameDeletePreviewViewModel : IPreviewViewModel
{
    /// <summary>預覽項目清單，繫結到抽幀刪除專用的 DataGrid。</summary>
    public IReadOnlyList<OperationPreviewItem> Items { get; }

    /// <inheritdoc/>
    public string Summary
    {
        get
        {
            var includedItems = Items.Where(i => i.IsIncluded).ToList();
            var deleteCount = includedItems.Count(i => i.ActionKind == OperationActionKind.Delete && !i.HasError);
            var copyCount = includedItems.Count(i => i.ActionKind == OperationActionKind.Copy && !i.HasError);
            var errorCount = Items.Count(i => i.HasError);
            var displayCount = includedItems.Count + errorCount;
            var actionText = copyCount > 0 ? "複製" : "刪除";
            var actionCount = copyCount > 0 ? copyCount : deleteCount;

            return errorCount > 0
                ? $"共 {displayCount} 個項目，預計{actionText} {actionCount} 個，{errorCount} 個錯誤"
                : $"共 {displayCount} 個項目，預計{actionText} {actionCount} 個";
        }
    }

    /// <inheritdoc/>
    public bool HasErrors => Items.Any(i => i.HasError);

    /// <inheritdoc/>
    public bool HasExecutableItems =>
        Items.Any(i =>
            i.IsIncluded &&
            i.ActionKind is OperationActionKind.Delete or OperationActionKind.Copy &&
            !i.HasError);

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    public FrameDeletePreviewViewModel(IReadOnlyList<OperationPreviewItem> items)
    {
        Items = items;

        foreach (var item in Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
        }
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OperationPreviewItem.IsIncluded))
        {
            return;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasErrors)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasExecutableItems)));
    }
}
