namespace FrameFileTool.Models;

/// <summary>
/// 檔案操作的強型別動作種類，供 planner、executor 與 ViewModel 判斷行為。
/// </summary>
public enum OperationActionKind
{
    /// <summary>不執行任何操作，原地保留。</summary>
    Keep,

    /// <summary>移到回收桶。</summary>
    Delete,

    /// <summary>批次改名。</summary>
    Rename,

    /// <summary>複製到另一個位置。</summary>
    Copy,

    /// <summary>批次縮放尺寸。</summary>
    Resize,

    /// <summary>參數或計畫本身有誤，無法執行。</summary>
    Error,
}

/// <summary>
/// 檔案操作動作的顯示文字轉換。
/// </summary>
public static class OperationAction
{
    /// <summary>取得指定動作種類的繁體中文顯示文字。</summary>
    public static string ToDisplayText(OperationActionKind kind) => kind switch
    {
        OperationActionKind.Delete => "刪除",
        OperationActionKind.Keep => "保留",
        OperationActionKind.Rename => "改名",
        OperationActionKind.Copy => "複製",
        OperationActionKind.Resize => "縮放",
        OperationActionKind.Error => "錯誤",
        _ => string.Empty,
    };
}
