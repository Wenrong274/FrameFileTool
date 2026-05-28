namespace FrameFileTool.Models;

/// <summary>
/// 拖放匯入檔案或資料夾後的結果。
/// </summary>
public sealed record FileImportResult(
    IReadOnlyList<FileItem> Files,
    IReadOnlyList<string> Errors);
