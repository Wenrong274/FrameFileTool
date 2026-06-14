namespace FrameFileTool.Models;

/// <summary>
/// 輸出資料夾解析結果。resolver 只回傳資料，不執行檔案系統副作用。
/// </summary>
public sealed record ResolvedOutputFolder(
    string TargetFolderPath,
    bool WasAutoRedirected,
    string? LogMessage);
