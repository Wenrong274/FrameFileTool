namespace FrameFileTool.Models;

/// <summary>縮放後的輸出位置模式。</summary>
public enum ResizeOutputMode
{
    /// <summary>
    /// 覆寫原始檔案。破壞性操作，執行前 UI 須顯示警示。
    /// </summary>
    Overwrite,

    /// <summary>
    /// 輸出至使用者指定資料夾，原始檔案不受影響。
    /// </summary>
    TargetFolder,
}
