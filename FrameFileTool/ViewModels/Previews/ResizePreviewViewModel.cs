using System.ComponentModel;
using FrameFileTool.Models;

namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 批次縮放操作的預覽結果，供 DataTemplate 顯示含尺寸欄位的 DataGrid。
/// 持有 <see cref="ResizePreviewItem"/> 清單，欄位包含原始尺寸與縮放後尺寸。
/// </summary>
public sealed class ResizePreviewViewModel : IPreviewViewModel
{
    /// <summary>預覽項目清單，繫結到批次縮放專用的 DataGrid。</summary>
    public IReadOnlyList<ResizePreviewItem> Items { get; }

    /// <inheritdoc/>
    public string Summary
    {
        get
        {
            var includedItems = Items.Where(i => i.IsIncluded).ToList();
            var resizeCount = includedItems.Count(i => i.Action == OperationAction.Resize && !i.HasError);
            var errorCount = Items.Count(i => i.HasError);
            var displayCount = includedItems.Count + errorCount;

            return errorCount > 0
                ? $"共 {displayCount} 個項目，預計縮放 {resizeCount} 個，{errorCount} 個錯誤（執行已停用）"
                : $"共 {displayCount} 個項目，預計縮放 {resizeCount} 個";
        }
    }

    /// <inheritdoc/>
    public bool HasErrors => Items.Any(i => i.HasError);

    /// <inheritdoc/>
    public bool HasExecutableItems =>
        Items.Any(i => i.IsIncluded && i.Action == OperationAction.Resize && !i.HasError);

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    public ResizePreviewViewModel(IReadOnlyList<ResizePreviewItem> items)
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
