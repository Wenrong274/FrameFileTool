using FrameFileTool.Models;

namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 抽幀刪除操作的預覽結果，供 DataTemplate 顯示對應欄位。
/// </summary>
public sealed class FrameDeletePreviewViewModel : PreviewViewModelBase<OperationPreviewItem>
{
    /// <inheritdoc/>
    public override string Summary
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
    public override bool HasErrors => Items.Any(i => i.HasError);

    /// <inheritdoc/>
    public override bool HasExecutableItems =>
        Items.Any(i =>
            i.IsIncluded &&
            i.ActionKind is OperationActionKind.Delete or OperationActionKind.Copy &&
            !i.HasError);

    public FrameDeletePreviewViewModel(IReadOnlyList<OperationPreviewItem> items)
        : base(items)
    {
    }
}
