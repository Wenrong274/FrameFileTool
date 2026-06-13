using FrameFileTool.Models;

namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 批次降噪操作的預覽結果。
/// 批次降噪固定覆寫原檔，項目不需要工具專屬欄位，直接使用共用的
/// <see cref="OperationPreviewItem"/>。
/// </summary>
public sealed class DenoisePreviewViewModel : PreviewViewModelBase<OperationPreviewItem>
{
    /// <inheritdoc/>
    public override string Summary
    {
        get
        {
            var includedItems = Items.Where(i => i.IsIncluded).ToList();
            var denoiseCount = includedItems.Count(i => i.ActionKind == OperationActionKind.Denoise && !i.HasError);
            var errorCount = Items.Count(i => i.HasError);
            var displayCount = includedItems.Count + errorCount;

            return errorCount > 0
                ? $"共 {displayCount} 個項目，預計降噪 {denoiseCount} 個（覆寫原檔），{errorCount} 個錯誤（執行已停用）"
                : $"共 {displayCount} 個項目，預計降噪 {denoiseCount} 個（覆寫原檔）";
        }
    }

    /// <inheritdoc/>
    public override bool HasErrors => Items.Any(i => i.HasError);

    /// <inheritdoc/>
    public override bool HasExecutableItems =>
        Items.Any(i => i.IsIncluded && i.ActionKind == OperationActionKind.Denoise && !i.HasError);

    public DenoisePreviewViewModel(IReadOnlyList<OperationPreviewItem> items)
        : base(items)
    {
    }
}
