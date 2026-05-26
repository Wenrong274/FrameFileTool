namespace FrameFileTool.Models;

/// <summary>
/// 檔案掃描結果，包含成功取得的檔案與掃描期間可復原的錯誤訊息。
/// </summary>
public sealed record FileScanResult(
    IReadOnlyList<FileItem> Files,
    IReadOnlyList<string> Errors);
