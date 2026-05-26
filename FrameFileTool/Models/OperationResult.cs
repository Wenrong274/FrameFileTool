namespace FrameFileTool.Models;

/// <summary>
/// 檔案操作執行結果，由 <see cref="FrameFileTool.Services.FileOperationExecutor"/> 回傳。
/// 包含成功筆數與各錯誤的說明文字。
/// </summary>
public sealed class OperationResult
{
    /// <summary>執行成功的檔案數量。</summary>
    public int SuccessCount { get; set; }

    /// <summary>因動作不符合或預覽含錯誤而略過的項目數量。</summary>
    public int SkippedCount { get; set; }

    /// <summary>是否由使用者取消執行。</summary>
    public bool Canceled { get; set; }

    /// <summary>執行失敗的檔案數量。</summary>
    public int FailureCount => Errors.Count;

    /// <summary>執行失敗的錯誤訊息清單，格式為「檔名: 原因」。</summary>
    public List<string> Errors { get; } = new();
}
