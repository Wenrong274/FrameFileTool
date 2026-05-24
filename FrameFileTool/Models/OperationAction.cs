namespace FrameFileTool.Models;

/// <summary>
/// 檔案操作的動作名稱常數，供 planner、executor 與 ViewModel 共用，
/// 避免各處散落相同的魔法字串。
/// </summary>
public static class OperationAction
{
    /// <summary>移到回收桶。</summary>
    public const string Delete = "刪除";

    /// <summary>不執行任何操作，原地保留。</summary>
    public const string Keep = "保留";

    /// <summary>批次改名。</summary>
    public const string Rename = "改名";

    /// <summary>參數或計畫本身有誤，無法執行。</summary>
    public const string Error = "錯誤";
}
