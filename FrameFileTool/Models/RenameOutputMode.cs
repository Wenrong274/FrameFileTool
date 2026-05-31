namespace FrameFileTool.Models;

/// <summary>批次改名工具的輸出模式。</summary>
public enum RenameOutputMode
{
    /// <summary>在原始資料夾內直接改名。</summary>
    RenameInPlace,

    /// <summary>複製到指定資料夾並套用新檔名，來源檔案不變更。</summary>
    CopyToTargetFolder,
}
