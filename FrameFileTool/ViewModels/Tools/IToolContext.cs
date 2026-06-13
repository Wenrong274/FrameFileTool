using FrameFileTool.Models;
using FrameFileTool.ViewModels.Previews;

namespace FrameFileTool.ViewModels.Tools;

/// <summary>
/// 工具 ViewModel 與 <see cref="MainViewModel"/> 之間的協調介面。
/// 工具透過此介面存取共用狀態（檔案清單、當前預覽、log）與共用流程
/// （選目標資料夾、衝突檢查、執行後重掃），不直接相依 MainViewModel 具體型別，
/// 測試時可用 mock 隔離。
/// </summary>
internal interface IToolContext
{
    /// <summary>目前選擇的來源資料夾，作為目標資料夾選擇器的起始路徑。</summary>
    public string SelectedFolder { get; }

    /// <summary>當前預覽結果。工具觸發預覽或執行前重新規劃時設定。</summary>
    public IPreviewViewModel? CurrentPreview { get; set; }

    /// <summary>是否正在準備預覽資料（縮放工具的非同步預覽使用）。</summary>
    public bool IsPreparingPreview { get; set; }

    /// <summary>預覽準備中顯示於摘要列的狀態文字。</summary>
    public string PreviewBusyText { get; set; }

    /// <summary>檔案清單是否非空，供 CanExecute 判斷。</summary>
    public bool HasFiles { get; }

    /// <summary>批次縮放或批次降噪是否執行中。為 true 時所有工具的執行命令都應停用。</summary>
    public bool IsBatchExecuting { get; }

    /// <summary>取得目前檔案清單的快照。</summary>
    public IReadOnlyList<FileItem> SnapshotFiles();

    /// <summary>設定當前預覽並記錄 log。</summary>
    public void SetCurrentPreview(IPreviewViewModel preview, string logMessage);

    /// <summary>將訊息寫入操作 log。</summary>
    public void AddLog(string message);

    /// <summary>將執行結果中的錯誤逐筆寫入 log。</summary>
    public void AddErrors(OperationResult result);

    /// <summary>通知所有命令重新評估 CanExecute。</summary>
    public void RefreshCommands();

    /// <summary>執行完成後重新掃描，保留使用者剔除的檔案排除清單。</summary>
    public void RescanKeepingExclusions();

    /// <summary>選擇目標資料夾；使用者取消時記錄 log 並回傳 null。</summary>
    public string? PickTargetFolderOrLogCancel(string operationName);

    /// <summary>
    /// 將重新規劃後的預覽套用為當前預覽；
    /// 含衝突或路徑錯誤時記錄 log、刷新命令並回傳 false。
    /// </summary>
    public bool ApplyPlannedPreviewOrLogConflict(string operationName, IPreviewViewModel plannedPreview);

    /// <summary>
    /// 同步版的完整複製前置流程：選資料夾 → 重新規劃 → 衝突檢查。
    /// 任一步驟中止時回傳 null。
    /// </summary>
    public TPreview? PrepareCopyToTargetPreview<TPreview>(
        string operationName,
        Func<string, TPreview> replanForTarget)
        where TPreview : class, IPreviewViewModel;

    /// <summary>工具設定變更時通知，讓協調者使對應工具的預覽失效並即時重建。</summary>
    public void NotifySettingChanged(PreviewTool tool);

    /// <summary>查詢指定目標路徑中實際已存在的檔案路徑集合。</summary>
    public IReadOnlySet<string> GetExistingPaths(IEnumerable<string> targetPaths);
}
