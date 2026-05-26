namespace FrameFileTool.Models;

/// <summary>
/// 代表單一檔案的操作預覽項目，供 DataGrid 顯示與 executor 執行使用。
/// 所有屬性為 init-only，建立後不可變更。
/// </summary>
public class OperationPreviewItem
{
    /// <summary>在預覽清單中的序號（從 1 開始）。</summary>
    public int Index { get; init; }

    /// <summary>來源檔案的完整路徑。</summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>原始檔名（不含目錄）。</summary>
    public string OriginalName { get; init; } = string.Empty;

    /// <summary>
    /// 操作動作，使用 <see cref="OperationAction"/> 常數。
    /// 例如：刪除、保留、改名、錯誤。
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// 目標檔名（改名時）或說明文字（刪除時為「移到回收桶」）。
    /// </summary>
    public string TargetName { get; init; } = string.Empty;

    /// <summary>操作狀態說明，供使用者確認計畫內容。</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// 是否有錯誤。為 <see langword="true"/> 時，executor 會略過此項目。
    /// </summary>
    public bool HasError { get; init; }
}
