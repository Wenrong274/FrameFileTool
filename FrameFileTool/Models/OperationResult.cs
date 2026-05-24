namespace FrameFileTool.Models;

/// <summary>
/// 檔案操作執行結果，包含成功筆數與錯誤清單。
/// </summary>
public sealed class OperationResult
{
    public int SuccessCount { get; set; }
    public List<string> Errors { get; } = new();
}
