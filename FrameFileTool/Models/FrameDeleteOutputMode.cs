namespace FrameFileTool.Models;

/// <summary>抽幀工具的輸出模式。</summary>
public enum FrameDeleteOutputMode
{
    /// <summary>將符合間隔規則的檔案移到回收桶。</summary>
    DeleteToRecycleBin,

    /// <summary>將保留幀複製到指定資料夾，來源檔案不變更。</summary>
    CopyKeptToTargetFolder,
}
