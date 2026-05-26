namespace FrameFileTool.Models;

/// <summary>
/// 縮放操作的預覽項目，繼承 <see cref="OperationPreviewItem"/> 並加入尺寸資訊。
/// 尺寸欄位只對縮放操作有意義，因此與共用基底類別分開定義。
/// </summary>
public sealed class ResizePreviewItem : OperationPreviewItem
{
    /// <summary>
    /// 原始圖片尺寸字串，格式如「1920×1080」。
    /// 來源圖片無法讀取尺寸時為空字串。
    /// </summary>
    public string OriginalDimensions { get; init; } = string.Empty;

    /// <summary>
    /// 縮放後預期尺寸字串，格式如「960×540」。
    /// 來源尺寸未知時為空字串。
    /// </summary>
    public string TargetDimensions { get; init; } = string.Empty;
}
