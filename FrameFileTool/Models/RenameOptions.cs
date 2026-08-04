namespace FrameFileTool.Models;

/// <summary>
/// 批次改名操作的所有參數，為 immutable record。
/// 由 ViewModel 建立並傳入 RenamePlanner，
/// 讓 <c>Plan</c> 與 <c>ProjectTargetPaths</c> 永遠共用同一組命名公式。
/// </summary>
/// <param name="Template">
/// 命名樣板。<c>[</c> 加一個以上 <c>#</c> 再加 <c>]</c> 為編號槽位，<c>#</c> 數量即補零位數；
/// 其餘所有字元一律視為字面字元，不提供跳脫語法。
/// </param>
/// <param name="StartIndex">重新編號時的起始編號；<paramref name="UseOriginalNumber"/> 為 true 時不使用。</param>
/// <param name="UseOriginalNumber">
/// 是否沿用原檔名的編號。為 true 時改用原檔名最後一組連續數字，
/// 該數字原封不動代入，樣板的補零位數不生效。
/// </param>
/// <param name="OutputMode">輸出模式：原地改名或複製到目標資料夾。</param>
/// <param name="TargetFolderPath">複製模式的目標資料夾；空字串代表執行時才選擇。</param>
public sealed record RenameOptions(
    string Template,
    int StartIndex = 0,
    bool UseOriginalNumber = false,
    RenameOutputMode OutputMode = RenameOutputMode.RenameInPlace,
    string TargetFolderPath = "");
