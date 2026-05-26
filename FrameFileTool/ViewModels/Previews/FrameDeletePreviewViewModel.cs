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
    public string Summary { get; }

    /// <inheritdoc/>
    public bool HasErrors { get; }

    public FrameDeletePreviewViewModel(IReadOnlyList<OperationPreviewItem> items)
    {
        Items = items;

        var deleteCount = items.Count(i => i.Action == OperationAction.Delete && !i.HasError);
        var errorCount = items.Count(i => i.HasError);

        HasErrors = errorCount > 0;
        Summary = HasErrors
            ? $"共 {items.Count} 個項目，預計刪除 {deleteCount} 個，{errorCount} 個錯誤"
            : $"共 {items.Count} 個項目，預計刪除 {deleteCount} 個";
    }
}
