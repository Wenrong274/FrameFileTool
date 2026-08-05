using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;
using FrameFileTool.ViewModels.Tools;

namespace FrameFileTool.ViewModels;

/// <summary>
/// 主視窗的 ViewModel：持有共用的掃描狀態（資料夾、副檔名、檔案清單、log、當前預覽），
/// 並協調四個工具 ViewModel（抽幀刪除、批次改名、批次縮放、批次降噪）。
/// 各工具的設定、預覽產生與執行命令由對應的 Tool ViewModel 持有；
/// 本類別實作 <see cref="IToolContext"/> 供工具回呼共用狀態與流程。
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IToolContext, IDisposable
{
    private readonly IFileScanner _scanner;
    private readonly IFolderPickerService _folderPicker;
    private readonly IFileExistenceService _fileExistenceService;
    private readonly IFileImportService _fileImportService;
    private readonly IUpdateService _updateService;
    private readonly IExternalLinkService _externalLinkService;
    private readonly CancellationTokenSource _updateCheckCts = new();
    private bool _isUpdateBannerDismissed;
    private readonly HashSet<string> _excludedFilePaths = new(StringComparer.OrdinalIgnoreCase);

    // ── 工具 ViewModels ──────────────────────────────────────

    /// <summary>抽幀刪除工具。</summary>
    public FrameDeleteToolViewModel FrameDeleteTool { get; }

    /// <summary>批次改名工具。</summary>
    public RenameToolViewModel RenameTool { get; }

    /// <summary>批次縮放工具。</summary>
    public ResizeToolViewModel ResizeTool { get; }

    /// <summary>批次降噪工具。</summary>
    public DenoiseToolViewModel DenoiseTool { get; }

    // ── 掃描選項 ──────────────────────────────────────────────

    [ObservableProperty]
    private string _selectedFolder = string.Empty;

    [ObservableProperty]
    private bool _includePng = true;

    [ObservableProperty]
    private bool _includeJpg = true;

    [ObservableProperty]
    private bool _includeJpeg = true;

    [ObservableProperty]
    private bool _includeWebp;

    [ObservableProperty]
    private bool _includeBmp;

    [ObservableProperty]
    private bool _includeSubfolders;

    // ── UI 狀態 ───────────────────────────────────────────────

    [ObservableProperty]
    private PreviewTool _selectedTool = PreviewTool.FrameDelete;

    /// <summary>
    /// TabControl.SelectedIndex 的 int 包裝，對應 SelectedTool enum 值。
    /// </summary>
    public int SelectedToolIndex
    {
        get => (int)SelectedTool;
        set => SelectedTool = (PreviewTool)value;
    }

    [ObservableProperty]
    private string _fileSummary = "尚未掃描";

    /// <summary>
    /// 當前預覽結果 ViewModel。null 代表尚未產生預覽。
    /// ContentControl 依此屬性的實際型別自動選擇對應的 DataTemplate。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    [NotifyPropertyChangedFor(nameof(HasPreviewErrors))]
    [NotifyPropertyChangedFor(nameof(PreviewSummary))]
    private IPreviewViewModel? _currentPreview;

    /// <summary>是否已產生預覽，供摘要列顏色狀態判斷。</summary>
    public bool HasPreview => CurrentPreview != null;

    /// <summary>當前預覽是否含有錯誤項目，供摘要列切換紅色警示。</summary>
    public bool HasPreviewErrors => CurrentPreview?.HasErrors ?? false;

    /// <summary>顯示於預覽摘要列的說明文字。</summary>
    public string PreviewSummary => IsPreparingPreview
        ? PreviewBusyText
        : CurrentPreview?.Summary ?? "載入圖片後將自動顯示預覽";

    /// <summary>
    /// 正在準備預覽資料。為 true 時停用掃描與預覽命令，避免重複操作。
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewSummary))]
    [NotifyCanExecuteChangedFor(nameof(BrowseFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScanFilesCommand))]
    private bool _isPreparingPreview;

    /// <summary>預覽準備中顯示於摘要列的狀態文字。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewSummary))]
    private string _previewBusyText = string.Empty;

    /// <summary>預覽區是否正處於拖放檔案或資料夾的 hover 狀態。</summary>
    [ObservableProperty]
    private bool _isPreviewDropTargetActive;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToDownloadPageCommand))]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _latestVersionText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GoToDownloadPageCommand))]
    private string _latestReleaseUrl = string.Empty;

    [ObservableProperty]
    private bool _isLogExpanded = false;

    /// <summary>格式列（掃描格式 pills）是否展開；預設收合，由使用者按「▼/▲ 格式」切換。</summary>
    [ObservableProperty]
    private bool _isSourceExpanded = false;

    /// <summary>是否已有來源檔案。</summary>
    public bool HasSource => Files.Count > 0;

    /// <summary>是否已填入資料夾路徑，驅動「重新掃描」按鈕顯示。</summary>
    public bool HasFolderPath => !string.IsNullOrEmpty(SelectedFolder);

    /// <summary>正在執行的更新檢查 Task，供測試層 await 結果。</summary>
    internal Task UpdateCheckTask { get; private set; } = Task.CompletedTask;

    public MainViewModel(
        IFileScanner scanner,
        IFrameDeletePlanner frameDeletePlanner,
        IRenamePlanner renamePlanner,
        IFileOperationExecutor executor,
        IFolderPickerService folderPicker,
        IImageResizeExecutor resizeExecutor,
        IResizePreviewService resizePreviewService,
        IOutputFolderResolver outputFolderResolver,
        IDenoisePlanner denoisePlanner,
        IDenoiseExecutor denoiseExecutor,
        IDenoisePreviewService denoisePreviewService,
        IFileExistenceService fileExistenceService,
        IFileImportService fileImportService,
        IUpdateService updateService,
        IExternalLinkService externalLinkService,
        TimeSpan debounceDelay = default)
    {
        _scanner = scanner;
        _folderPicker = folderPicker;
        _fileExistenceService = fileExistenceService;
        _fileImportService = fileImportService;
        _updateService = updateService;
        _externalLinkService = externalLinkService;

        // 工具 ViewModel 以本類別為 IToolContext 組合，
        // 組合根在此而非 DI 容器，避免 MainViewModel 與工具間的循環註冊。
        FrameDeleteTool = new FrameDeleteToolViewModel(frameDeletePlanner, executor, this);
        RenameTool = new RenameToolViewModel(renamePlanner, executor, this);
        ResizeTool = new ResizeToolViewModel(
            resizeExecutor,
            resizePreviewService,
            outputFolderResolver,
            this,
            debounceDelay == default ? TimeSpan.FromMilliseconds(350) : debounceDelay);
        DenoiseTool = new DenoiseToolViewModel(denoisePlanner, denoiseExecutor, denoisePreviewService, this);
        Files.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasSource));
    }

    /// <summary>掃描結果檔案清單，繫結到掃描結果區塊。</summary>
    public ObservableCollection<FileItem> Files { get; } = [];

    /// <summary>操作 log，最新訊息在最上方。</summary>
    public ObservableCollection<string> Logs { get; } = [];

    // ── Commands ──────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanBrowseOrScan))]
    private void BrowseFolder()
    {
        var folder = _folderPicker.PickFolder(SelectedFolder);
        if (folder is null)
        {
            return;
        }

        SelectedFolder = folder;
        AddLog($"已選擇資料夾：{folder}");
        ScanFiles();
    }

    [RelayCommand(CanExecute = nameof(CanBrowseOrScan))]
    private void ScanFiles()
    {
        RefreshScanFilesCore(keepExclusions: false);
    }

    private void RefreshScanFilesCore(bool keepExclusions)
    {
        if (!keepExclusions)
        {
            _excludedFilePaths.Clear();
        }

        Files.Clear();
        CurrentPreview = null;

        var extensions = GetSelectedExtensions();
        var scanResult = _scanner.Scan(SelectedFolder, extensions, IncludeSubfolders);

        foreach (var file in scanResult.Files)
        {
            if (!_excludedFilePaths.Contains(file.FullPath))
            {
                Files.Add(file);
            }
        }

        FileSummary = $"已掃描 {Files.Count} 個圖片檔";
        AddLog($"{FileSummary}。資料夾：{SelectedFolder}");
        foreach (var error in scanResult.Errors)
        {
            AddLog($"掃描錯誤：{error}");
        }

        RefreshCommands();
        TriggerLivePreviewForCurrentTool();
    }

    [RelayCommand(CanExecute = nameof(CanBrowseOrScan))]
    private async Task ImportDroppedPaths(string[]? paths)
    {
        if (paths is null || paths.Length == 0)
        {
            return;
        }

        var existingPaths = Files
            .Select(file => file.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = _fileImportService.Import(
            paths,
            GetSelectedExtensions(),
            IncludeSubfolders,
            existingPaths);

        if (result.Files.Count > 0)
        {
            CurrentPreview = null;
            foreach (var file in result.Files)
            {
                _excludedFilePaths.Remove(file.FullPath); // 重新啟用手動拖入的檔案
                Files.Add(file);
            }

            FileSummary = $"已載入 {Files.Count} 個圖片檔";
        }

        AddLog($"拖放匯入完成：新增 {result.Files.Count} 個圖片檔，略過或錯誤 {result.Errors.Count} 個。");
        foreach (var error in result.Errors)
        {
            AddLog($"拖放略過：{error}");
        }

        RefreshCommands();

        if (result.Files.Count > 0)
        {
            TriggerLivePreviewForCurrentTool();
        }
    }

    [RelayCommand]
    private void ClearLog() => Logs.Clear();

    [RelayCommand]
    private void ClearFolderAndFiles()
    {
        SelectedFolder = string.Empty;
        _excludedFilePaths.Clear();
        Files.Clear();
        ClearCurrentPreview();
    }

    [RelayCommand]
    private void RemoveFile(object parameter)
    {
        if (parameter is null)
        {
            return;
        }

        FileItem? fileItem = null;
        if (parameter is FileItem fi)
        {
            fileItem = fi;
        }
        else if (parameter is OperationPreviewItem op)
        {
            fileItem = Files.FirstOrDefault(f => f.FullPath == op.FullPath);
        }

        if (fileItem is not null)
        {
            _excludedFilePaths.Add(fileItem.FullPath); // 記錄被剔除的 FullPath
            Files.Remove(fileItem);
        }

        if (Files.Count == 0)
        {
            ClearCurrentPreview();
        }
        else
        {
            TriggerLivePreviewForCurrentTool();
        }
    }

    [RelayCommand]
    private void ToggleLog() => IsLogExpanded = !IsLogExpanded;

    [RelayCommand]
    private void ToggleSource() => IsSourceExpanded = !IsSourceExpanded;

    [RelayCommand(CanExecute = nameof(CanGoToDownloadPage))]
    private void GoToDownloadPage() =>
        _externalLinkService.Open(LatestReleaseUrl);

    [RelayCommand]
    private void DismissUpdateBanner()
    {
        _isUpdateBannerDismissed = true;
        IsUpdateAvailable = false;
        GoToDownloadPageCommand.NotifyCanExecuteChanged();
    }

    // ── 預覽失效管理 ──────────────────────────────────────────

    partial void OnSelectedFolderChanged(string value)
    {
        _excludedFilePaths.Clear();
        OnPropertyChanged(nameof(HasFolderPath));
        InvalidateAnyPreview();
    }

    partial void OnIncludePngChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeJpgChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeJpegChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeWebpChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeBmpChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeSubfoldersChanged(bool value) => InvalidateAnyPreview();

    partial void OnSelectedToolChanged(PreviewTool value)
    {
        OnPropertyChanged(nameof(SelectedToolIndex));
        if (value != PreviewTool.Resize)
        {
            ResizeTool.CancelPendingPreview();
        }

        if (CurrentPreview is not null && GetPreviewTool(CurrentPreview) != value)
        {
            if (GetPreviewTool(CurrentPreview) == PreviewTool.Resize)
            {
                ResizeTool.CancelPendingPreview();
            }

            ClearCurrentPreview();
        }

        TriggerLivePreviewForCurrentTool();
    }

    partial void OnCurrentPreviewChanged(IPreviewViewModel? oldValue, IPreviewViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnCurrentPreviewPropertyChanged;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnCurrentPreviewPropertyChanged;
        }

        // 工具的執行命令以 CurrentPreview 型別判斷 CanExecute，
        // 屬性attribute無法跨 ViewModel 通知，改在此明確刷新。
        RefreshCommands();
    }

    private void OnCurrentPreviewPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(IPreviewViewModel.Summary)
            and not nameof(IPreviewViewModel.HasErrors)
            and not nameof(IPreviewViewModel.HasExecutableItems))
        {
            return;
        }

        OnPropertyChanged(nameof(PreviewSummary));
        OnPropertyChanged(nameof(HasPreviewErrors));
        RefreshCommands();
    }

    // ── 工具協調（IToolContext 共用流程實作） ────────────────

    /// <summary>使用者變更共用輸入時，任何既有預覽都不再可信。</summary>
    private void InvalidateAnyPreview()
    {
        ResizeTool.CancelPendingPreview();
        ClearCurrentPreview();
    }

    /// <summary>使用者變更特定工具設定時，清除該工具的既有預覽並即時重新產生。</summary>
    private void InvalidatePreviewFor(PreviewTool tool)
    {
        if (tool == PreviewTool.Resize)
        {
            ResizeTool.CancelPendingPreview();
        }

        if (CurrentPreview is not null && GetPreviewTool(CurrentPreview) == tool)
        {
            ClearCurrentPreview();
        }

        if (SelectedTool == tool)
        {
            TriggerLivePreviewForCurrentTool();
        }
    }

    /// <summary>
    /// 依當前選取的工具觸發即時預覽。
    /// 抽幀刪除與批次改名同步計算；批次縮放以 debounce 觸發非同步預覽。
    /// 若 Files 為空或正在執行縮放，則不觸發。
    /// </summary>
    private void TriggerLivePreviewForCurrentTool()
    {
        if (IsBatchExecuting || Files.Count == 0)
            return;

        switch (SelectedTool)
        {
            case PreviewTool.FrameDelete:
                FrameDeleteTool.TriggerPreview();
                break;
            case PreviewTool.Rename:
                RenameTool.TriggerPreview();
                break;
            case PreviewTool.Resize:
                ResizeTool.TriggerPreviewDebounced();
                break;
            case PreviewTool.Denoise:
                DenoiseTool.TriggerPreview();
                break;
        }
    }

    private void ClearCurrentPreview()
    {
        CurrentPreview = null;
        RefreshCommands();
    }

    private void SetCurrentPreview(IPreviewViewModel preview, string logMessage)
    {
        CurrentPreview = preview;
        AddLog(logMessage);
        RefreshCommands();
    }

    private string? PickTargetFolderOrLogCancel(string operationName)
    {
        var targetFolder = _folderPicker.PickFolder(SelectedFolder);
        if (targetFolder is null)
        {
            AddLog($"{operationName}已取消：未選擇目標資料夾。");
        }

        return targetFolder;
    }

    private bool ApplyPlannedPreviewOrLogConflict(string operationName, IPreviewViewModel plannedPreview)
    {
        CurrentPreview = plannedPreview;

        if (plannedPreview.HasErrors)
        {
            AddLog($"{operationName}已停止：目標資料夾存在衝突或路徑錯誤。");
            RefreshCommands();
            return false;
        }

        return true;
    }

    private TPreview? PrepareCopyToTargetPreview<TPreview>(
        string operationName,
        Func<string, TPreview> replanForTarget)
        where TPreview : class, IPreviewViewModel
    {
        var targetFolder = PickTargetFolderOrLogCancel(operationName);
        if (targetFolder is null)
        {
            return null;
        }

        var plannedPreview = replanForTarget(targetFolder);
        return ApplyPlannedPreviewOrLogConflict(operationName, plannedPreview) ? plannedPreview : null;
    }

    private static PreviewTool GetPreviewTool(IPreviewViewModel preview) => preview switch
    {
        FrameDeletePreviewViewModel => PreviewTool.FrameDelete,
        RenamePreviewViewModel => PreviewTool.Rename,
        ResizePreviewViewModel => PreviewTool.Resize,
        DenoisePreviewViewModel => PreviewTool.Denoise,
        _ => throw new InvalidOperationException("未知的預覽型別。"),
    };

    // ── IToolContext 明確實作 ────────────────────────────────

    bool IToolContext.HasFiles => Files.Count > 0;

    bool IToolContext.IsBatchExecuting => IsBatchExecuting;

    IReadOnlyList<FileItem> IToolContext.SnapshotFiles() => Files.ToList();

    void IToolContext.SetCurrentPreview(IPreviewViewModel preview, string logMessage) =>
        SetCurrentPreview(preview, logMessage);

    void IToolContext.AddLog(string message) => AddLog(message);

    void IToolContext.AddErrors(OperationResult result) => AddErrors(result);

    void IToolContext.RefreshCommands() => RefreshCommands();

    void IToolContext.RescanKeepingExclusions()
    {
        // 拖放匯入的檔案沒有來源資料夾，重掃只會把整份清單清空，此時維持現狀。
        if (HasFolderPath)
        {
            RefreshScanFilesCore(keepExclusions: true);
        }
    }

    string? IToolContext.PickTargetFolderOrLogCancel(string operationName) =>
        PickTargetFolderOrLogCancel(operationName);

    bool IToolContext.ApplyPlannedPreviewOrLogConflict(string operationName, IPreviewViewModel plannedPreview) =>
        ApplyPlannedPreviewOrLogConflict(operationName, plannedPreview);

    TPreview? IToolContext.PrepareCopyToTargetPreview<TPreview>(
        string operationName,
        Func<string, TPreview> replanForTarget)
        where TPreview : class =>
        PrepareCopyToTargetPreview(operationName, replanForTarget);

    void IToolContext.NotifySettingChanged(PreviewTool tool) => InvalidatePreviewFor(tool);

    IReadOnlySet<string> IToolContext.GetExistingPaths(IEnumerable<string> targetPaths) =>
        _fileExistenceService.GetExistingPaths(targetPaths);

    // ── 私有輔助方法 ──────────────────────────────────────────

    /// <summary>依勾選狀態收集目前啟用的副檔名清單。</summary>
    private IReadOnlyList<string> GetSelectedExtensions()
    {
        var extensions = new List<string>(5);

        if (IncludePng)
            extensions.Add(".png");
        if (IncludeJpg)
            extensions.Add(".jpg");
        if (IncludeJpeg)
            extensions.Add(".jpeg");
        if (IncludeWebp)
            extensions.Add(".webp");
        if (IncludeBmp)
            extensions.Add(".bmp");

        return extensions;
    }

    /// <summary>批次縮放或批次降噪是否執行中，期間所有非取消操作都應停用。</summary>
    private bool IsBatchExecuting => ResizeTool.IsResizing || DenoiseTool.IsDenoising;

    private bool CanBrowseOrScan() => !IsBatchExecuting && !IsPreparingPreview;

    private bool CanGoToDownloadPage() =>
        IsUpdateAvailable && !string.IsNullOrWhiteSpace(LatestReleaseUrl);

    /// <summary>
    /// 啟動背景更新檢查。建構式不做副作用，
    /// 由 View 在初始化 wiring 階段（MainWindow 建構）明確呼叫。
    /// </summary>
    public void StartUpdateCheck() =>
        UpdateCheckTask = RunUpdateCheckAsync(_updateService, _updateCheckCts.Token);

    private async Task RunUpdateCheckAsync(IUpdateService updateService, CancellationToken token)
    {
        try
        {
            var updateInfo = await updateService.CheckForUpdateAsync(token);
            if (_isUpdateBannerDismissed || !updateInfo.HasUpdate)
            {
                return;
            }

            LatestVersionText = updateInfo.LatestVersion;
            LatestReleaseUrl = updateInfo.ReleaseUrl;
            IsUpdateAvailable = true;
        }
        catch (OperationCanceledException)
        {
            IsUpdateAvailable = false;
        }
        catch (Exception ex)
        {
            AddLog($"更新檢查失敗：{ex.GetType().Name}");
            IsUpdateAvailable = false;
        }
    }

    public void Dispose()
    {
        _updateCheckCts.Cancel();
        _updateCheckCts.Dispose();
        DenoiseTool.Dispose();
    }

    /// <summary>將訊息插入 log 最上方，附上時間戳記。</summary>
    private void AddLog(string message) =>
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");

    private void AddErrors(OperationResult result)
    {
        foreach (var error in result.Errors)
        {
            AddLog($"錯誤：{error}");
        }
    }

    /// <summary>
    /// 明確通知所有帶 CanExecute 的 command 重新評估狀態，
    /// 包含四個工具 ViewModel 的命令。
    /// </summary>
    private void RefreshCommands()
    {
        BrowseFolderCommand.NotifyCanExecuteChanged();
        ScanFilesCommand.NotifyCanExecuteChanged();
        ImportDroppedPathsCommand.NotifyCanExecuteChanged();
        FrameDeleteTool.RefreshCommands();
        RenameTool.RefreshCommands();
        ResizeTool.RefreshCommands();
        DenoiseTool.RefreshCommands();
    }
}
