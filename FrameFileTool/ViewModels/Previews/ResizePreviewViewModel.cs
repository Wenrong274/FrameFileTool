using FrameFileTool.Models;

namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 批次縮放操作的預覽結果，供 DataTemplate 顯示含尺寸欄位的 DataGrid。
/// 持有 <see cref="ResizePreviewItem"/> 清單，欄位包含原始尺寸與縮放後尺寸。
/// </summary>
public sealed class ResizePreviewViewModel : PreviewViewModelBase<ResizePreviewItem>
{
    /// <inheritdoc/>
    public override string Summary
    {
        get
        {
            var includedItems = Items.Where(i => i.IsIncluded).ToList();
            var resizeCount = includedItems.Count(i => i.ActionKind == OperationActionKind.Resize && !i.HasError);
            var errorCount = Items.Count(i => i.HasError);
            var displayCount = includedItems.Count + errorCount;

            return errorCount > 0
                ? $"共 {displayCount} 個項目，預計縮放 {resizeCount} 個，{errorCount} 個錯誤（執行已停用）"
                : $"共 {displayCount} 個項目，預計縮放 {resizeCount} 個";
        }
    }

    /// <inheritdoc/>
    public override bool HasErrors => Items.Any(i => i.HasError);

    /// <inheritdoc/>
    public override bool HasExecutableItems =>
        Items.Any(i => i.IsIncluded && i.ActionKind == OperationActionKind.Resize && !i.HasError);

    public ResizePreviewViewModel(IReadOnlyList<ResizePreviewItem> items)
        : base(items)
    {
    }
}
