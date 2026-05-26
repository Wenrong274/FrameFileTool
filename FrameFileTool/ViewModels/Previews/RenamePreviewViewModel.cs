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
    public string Summary { get; }

    /// <inheritdoc/>
    public bool HasErrors { get; }

    public RenamePreviewViewModel(IReadOnlyList<OperationPreviewItem> items)
    {
        Items = items;

        var renameCount = items.Count(i => i.Action == OperationAction.Rename && !i.HasError);
        var errorCount = items.Count(i => i.HasError);

        HasErrors = errorCount > 0;
        Summary = HasErrors
            ? $"共 {items.Count} 個項目，預計改名 {renameCount} 個，{errorCount} 個錯誤（執行已停用）"
            : $"共 {items.Count} 個項目，預計改名 {renameCount} 個";
    }
}
