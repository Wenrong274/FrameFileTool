namespace FrameFileTool.Models;

/// <summary>
/// 主視窗工具的識別。
/// 與 Tab 的顯示順序無關：XAML 以 EnumToBoolConverter 將各 TabItem 的
/// IsSelected 繫結到對應的 enum 值，調整 Tab 順序不影響行為。
/// </summary>
public enum PreviewTool
{
    /// <summary>抽幀刪除。</summary>
    FrameDelete,

    /// <summary>批次改名。</summary>
    Rename,

    /// <summary>批次縮放。</summary>
    Resize,

    /// <summary>批次降噪。</summary>
    Denoise,
}
