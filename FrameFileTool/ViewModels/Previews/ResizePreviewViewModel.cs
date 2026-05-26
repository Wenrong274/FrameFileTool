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
    public string Summary { get; }

    /// <inheritdoc/>
    public bool HasErrors { get; }

    public ResizePreviewViewModel(IReadOnlyList<ResizePreviewItem> items)
    {
        Items = items;

        var resizeCount = items.Count(i => i.Action == OperationAction.Resize && !i.HasError);
        var errorCount = items.Count(i => i.HasError);

        HasErrors = errorCount > 0;
        Summary = HasErrors
            ? $"共 {items.Count} 個項目，預計縮放 {resizeCount} 個，{errorCount} 個錯誤（執行已停用）"
            : $"共 {items.Count} 個項目，預計縮放 {resizeCount} 個";
    }
}
