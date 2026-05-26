namespace FrameFileTool.Models;

/// <summary>縮放後的輸出位置模式。</summary>
public enum ResizeOutputMode
{
    /// <summary>
    /// 另存至原始資料夾下的子資料夾，原始檔案不受影響。
    /// </summary>
    Subfolder,

    /// <summary>
    /// 覆寫原始檔案。破壞性操作，執行前 UI 須顯示警示。
    /// </summary>
    Overwrite,
}
