using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;

namespace FrameFileTool.ViewModels;

/// <summary>
/// 主視窗的 ViewModel，管理 UI 狀態與 command 生命週期。
/// 所有商業邏輯均委派給對應的 service；ViewModel 只負責
/// 狀態同步、log 記錄與 command 的 CanExecute 管理。
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private enum PreviewTool
    {
        FrameDelete = 0,
        Rename = 1,
        Resize = 2,
    }

    private readonly IFileScanner _scanner;
    private readonly IFrameDeletePlanner _frameDeletePlanner;
    private readonly IRenamePlanner _renamePlanner;
    private readonly IFileOperationExecutor _executor;
    private readonly IFolderPickerService _folderPicker;
    private readonly IImageResizeExecutor _resizeExecutor;
    private readonly IResizePreviewService _resizePreviewService;
    private readonly IFileExistenceService _fileExistenceService;
    private readonly IFileImportService _fileImportService;
    private readonly IUpdateService _updateService;
    private readonly IExternalLinkService _externalLinkService;
    private readonly CancellationTokenSource _updateCheckCts = new();
    private bool _isUpdateBannerDismissed;

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

    // ── 抽幀設定 ──────────────────────────────────────────────

    [ObservableProperty]
    private int _frameDeleteInterval = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FrameDeleteExecuteButtonText))]
    private FrameDeleteOutputMode _frameDeleteOutputMode = FrameDeleteOutputMode.DeleteToRecycleBin;

    // ── 縮放設定 ──────────────────────────────────────────────

    [ObservableProperty]
    private ResizeMode _resizeMode = ResizeMode.ScaleFactor;

    [ObservableProperty]
    private double _scaleFactor = 0.5;

    public double ScaleFactorSliderMinimum => 0.1;

    public double ScaleFactorSliderMaximum => 4.0;

    public double ScaleFactorSliderSmallChange => 0.1;

    [ObservableProperty]
    private int _targetWidth;

    [ObservableProperty]
    private int _targetHeight;

    [ObservableProperty]
    private bool _keepAspectRatio = true;

    [ObservableProperty]
    private ResizeOutputMode _resizeOutputMode = ResizeOutputMode.TargetFolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResamplerHint))]
    private ResamplerType _selectedResampler = ResamplerType.Bicubic;

    // ── 改名設定 ──────────────────────────────────────────────

    [ObservableProperty]
    private string _renamePrefix = "Symbol_";

    [ObservableProperty]
    private int _renameStartIndex;

    [ObservableProperty]
    private int _renamePadding;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenameExecuteButtonText))]
    private RenameOutputMode _renameOutputMode = RenameOutputMode.RenameInPlace;

    // ── UI 狀態 ───────────────────────────────────────────────

    [ObservableProperty]
    private int _selectedToolIndex;

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
    [NotifyCanExecuteChangedFor(nameof(ExecuteFrameDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteRenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteResizeCommand))]
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

    // ── 縮放執行進度 ─────────────────────────────────────────

    /// <summary>
    /// 縮放正在執行中。為 true 時禁用所有非取消操作，防止 UI 衝突。
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BrowseFolderCommand))]
    [NotifyCanExecuteChangedFor(nameof(ScanFilesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteFrameDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteRenameCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExecuteResizeCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelResizeCommand))]
    private bool _isResizing;

    /// <summary>目前已完成的縮放筆數，供進度條的 Value 繫結。</summary>
    [ObservableProperty]
    private int _resizeProgressCurrent;

    /// <summary>本次縮放的總筆數，供進度條的 Maximum 繫結。</summary>
    [ObservableProperty]
    private int _resizeProgressTotal = 1;

    /// <summary>進度條下方的文字描述，例如「3 / 10  frame003.png」。</summary>
    [ObservableProperty]
    private string _resizeProgressText = string.Empty;

    private readonly TimeSpan _debounceDelay;

    private CancellationTokenSource? _resizeCts;
    private CancellationTokenSource? _resizePreviewCts;
    private long _resizePreviewRequestId;
    private readonly HashSet<string> _excludedFilePaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>正在執行的即時預覽 Task，供測試層 await 結果。</summary>
    internal Task LivePreviewTask { get; private set; } = Task.CompletedTask;

    /// <summary>正在執行的更新檢查 Task，供測試層 await 結果。</summary>
    internal Task UpdateCheckTask { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// 依選取的演算法顯示對應的使用建議說明，供 UI HintText 繫結。
    /// </summary>
    public string ResamplerHint => SelectedResampler switch
    {
        ResamplerType.Lanczos3 => "大幅縮小時保持文字與線條清晰",
        ResamplerType.CatmullRom => "放大時邊緣比一般用途更銳利",
        ResamplerType.NearestNeighbor => "整數倍縮放截圖，保持像素對齊",
        ResamplerType.MitchellNetravali => "線條圖需要最銳利邊緣時使用",
        _ => "大多數縮放情境的穩定選擇",
    };

    public string FrameDeleteExecuteButtonText => FrameDeleteOutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
        ? "執行複製"
        : "執行刪除";

    public string RenameExecuteButtonText => RenameOutputMode == RenameOutputMode.CopyToTargetFolder
        ? "執行複製改名"
        : "執行改名";

    public MainViewModel(
        IFileScanner scanner,
        IFrameDeletePlanner frameDeletePlanner,
        IRenamePlanner renamePlanner,
        IFileOperationExecutor executor,
        IFolderPickerService folderPicker,
        IImageResizeExecutor resizeExecutor,
        IResizePreviewService resizePreviewService,
        IFileExistenceService fileExistenceService,
        IFileImportService fileImportService,
        IUpdateService updateService,
        IExternalLinkService externalLinkService,
        TimeSpan debounceDelay = default)
    {
        _scanner = scanner;
        _frameDeletePlanner = frameDeletePlanner;
        _renamePlanner = renamePlanner;
        _executor = executor;
        _folderPicker = folderPicker;
        _resizeExecutor = resizeExecutor;
        _resizePreviewService = resizePreviewService;
        _fileExistenceService = fileExistenceService;
        _fileImportService = fileImportService;
        _updateService = updateService;
        _externalLinkService = externalLinkService;
        _debounceDelay = debounceDelay == default
            ? TimeSpan.FromMilliseconds(350)
            : debounceDelay;
        StartUpdateCheck();
    }

    /// <summary>掃描結果檔案清單，繫結到掃描結果區塊。</summary>
    public ObservableCollection<FileItem> Files { get; } = [];

    /// <summary>縮放演算法選項清單，供 ComboBox 繫結。</summary>
    public IReadOnlyList<ResamplerType> ResamplerOptions { get; } =
        Enum.GetValues<ResamplerType>().ToList();

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

    private void TriggerFrameDeletePreview()
    {
        var files = Files.ToList();
        var items = FrameDeleteOutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? _frameDeletePlanner.Plan(
                files,
                FrameDeleteInterval,
                FrameDeleteOutputMode,
                targetFolderPath: string.Empty)
            : _frameDeletePlanner.Plan(files, FrameDeleteInterval);
        if (items is null)
            return;

        var vm = new FrameDeletePreviewViewModel(items);
        var actionText = FrameDeleteOutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? "複製"
            : "刪除";
        var actionKind = FrameDeleteOutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? OperationActionKind.Copy
            : OperationActionKind.Delete;
        var copyNote = FrameDeleteOutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? "（目標資料夾待確認，衝突偵測將於執行時進行）"
            : string.Empty;
        SetCurrentPreview(
            vm,
            $"抽幀預覽完成：每 {FrameDeleteInterval} 張刪除 1 張，預計{actionText} {items.Count(i => i.ActionKind == actionKind && !i.HasError)} 個檔案。{copyNote}");
    }

    [RelayCommand(CanExecute = nameof(HasExecutableDeletePreview))]
    private void ExecuteFrameDelete()
    {
        if (CurrentPreview is not FrameDeletePreviewViewModel vm)
            return;

        OperationResult result;
        if (FrameDeleteOutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder)
        {
            var targetFolder = _folderPicker.PickFolder(SelectedFolder);
            if (targetFolder is null)
            {
                AddLog("抽幀複製已取消：未選擇目標資料夾。");
                return;
            }

            var plannedItems = _frameDeletePlanner.Plan(
                Files.ToList(),
                FrameDeleteInterval,
                FrameDeleteOutputMode.CopyKeptToTargetFolder,
                targetFolder,
                GetExistingTargetPaths(Files.ToList(), targetFolder, file => file.Name));

            var plannedPreview = new FrameDeletePreviewViewModel(plannedItems);
            CurrentPreview = plannedPreview;

            if (plannedPreview.HasErrors)
            {
                AddLog("抽幀複製已停止：目標資料夾存在衝突或路徑錯誤。");
                RefreshCommands();
                return;
            }

            result = _executor.CopyFilesToTargetFolder(plannedItems);
        }
        else
        {
            result = _executor.DeleteToRecycleBin(vm.Items);
        }

        AddLog(FrameDeleteOutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? $"抽幀複製完成：成功 {result.SuccessCount} 個檔案。"
            : $"抽幀執行完成：已移到回收桶 {result.SuccessCount} 個檔案。");
        AddErrors(result);
        RefreshScanFilesCore(keepExclusions: true);
    }

    private void TriggerRenamePreview()
    {
        var files = Files.ToList();
        var items = RenameOutputMode == RenameOutputMode.CopyToTargetFolder
            ? _renamePlanner.Plan(
                files,
                RenamePrefix,
                RenameStartIndex,
                RenamePadding,
                GetExistingRenameTargetPaths(files),
                RenameOutputMode,
                targetFolderPath: string.Empty)
            : _renamePlanner.Plan(
                files,
                RenamePrefix,
                RenameStartIndex,
                RenamePadding,
                files.Select(f => f.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase));
        if (items is null)
            return;

        var vm = new RenamePreviewViewModel(items);
        var actionText = RenameOutputMode == RenameOutputMode.CopyToTargetFolder ? "複製改名" : "改名";
        var actionKind = RenameOutputMode == RenameOutputMode.CopyToTargetFolder
            ? OperationActionKind.Copy
            : OperationActionKind.Rename;
        var actionCount = items.Count(i => i.ActionKind == actionKind && !i.HasError);
        var errorCount = items.Count(i => i.HasError);
        var renameCopyNote = RenameOutputMode == RenameOutputMode.CopyToTargetFolder
            ? "（目標資料夾待確認，衝突偵測將於執行時進行）"
            : string.Empty;
        SetCurrentPreview(
            vm,
            $"改名預覽完成：預計{actionText} {actionCount} 個檔案，錯誤 {errorCount} 個。{renameCopyNote}");
    }

    [RelayCommand(CanExecute = nameof(HasExecutableRenamePreview))]
    private void ExecuteRename()
    {
        if (CurrentPreview is not RenamePreviewViewModel vm)
            return;

        OperationResult result;
        if (RenameOutputMode == RenameOutputMode.CopyToTargetFolder)
        {
            var targetFolder = _folderPicker.PickFolder(SelectedFolder);
            if (targetFolder is null)
            {
                AddLog("複製改名已取消：未選擇目標資料夾。");
                return;
            }

            var plannedItems = _renamePlanner.Plan(
                Files.ToList(),
                RenamePrefix,
                RenameStartIndex,
                RenamePadding,
                GetExistingRenameTargetPaths(Files.ToList(), targetFolder),
                RenameOutputMode.CopyToTargetFolder,
                targetFolder);

            var plannedPreview = new RenamePreviewViewModel(plannedItems);
            CurrentPreview = plannedPreview;

            if (plannedPreview.HasErrors)
            {
                AddLog("複製改名已停止：目標資料夾存在衝突或路徑錯誤。");
                RefreshCommands();
                return;
            }

            result = _executor.CopyRenamedFilesToTargetFolder(plannedItems);
        }
        else
        {
            result = _executor.RenameFiles(vm.Items);
        }

        AddLog(RenameOutputMode == RenameOutputMode.CopyToTargetFolder
            ? $"複製改名完成：成功 {result.SuccessCount} 個檔案。"
            : $"改名執行完成：成功 {result.SuccessCount} 個檔案。");
        AddErrors(result);
        RefreshScanFilesCore(keepExclusions: true);
    }

    private void TriggerResizePreviewDebounced()
    {
        CancelResizePreviewRequest(clearBusyState: false);
        var cts = new CancellationTokenSource();
        _resizePreviewCts = cts;
        var requestId = ++_resizePreviewRequestId;
        LivePreviewTask = RunDelayedResizePreviewAsync(requestId, cts);
    }

    private async Task RunDelayedResizePreviewAsync(long requestId, CancellationTokenSource previewCts)
    {
        try
        {
            await Task.Delay(_debounceDelay, previewCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (previewCts.IsCancellationRequested || requestId != _resizePreviewRequestId)
            return;

        await RunResizePreviewCoreAsync(requestId, previewCts);
    }

    private async Task RunResizePreviewCoreAsync(long requestId, CancellationTokenSource previewCts)
    {
        var options = BuildResizeOptions();
        var files = Files.ToList();

        CurrentPreview = null;
        PreviewBusyText = "正在讀取圖片尺寸…";
        IsPreparingPreview = true;

        try
        {
            var items = await _resizePreviewService.BuildPreviewAsync(files, options, previewCts.Token);
            if (previewCts.IsCancellationRequested || requestId != _resizePreviewRequestId)
            {
                return;
            }

            var vm = new ResizePreviewViewModel(items);
            SetCurrentPreview(
                vm,
                $"縮放預覽完成：預計縮放 {items.Count(i => i.ActionKind == OperationActionKind.Resize && !i.HasError)} 個檔案，錯誤 {items.Count(i => i.HasError)} 個。");
        }
        catch (OperationCanceledException) when (previewCts.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_resizePreviewCts, previewCts))
            {
                _resizePreviewCts = null;
            }

            previewCts.Dispose();

            if (requestId == _resizePreviewRequestId)
            {
                IsPreparingPreview = false;
                PreviewBusyText = string.Empty;
                RefreshCommands();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(HasExecutableResizePreview))]
    private async Task ExecuteResize()
    {
        if (CurrentPreview is not ResizePreviewViewModel previewVm)
            return;

        var options = BuildResizeOptions();

        if (options.OutputMode == ResizeOutputMode.Overwrite)
        {
            AddLog("⚠ 覆寫模式：原始圖片將被取代，無法還原。");
        }
        else if (options.OutputMode == ResizeOutputMode.TargetFolder)
        {
            var targetFolder = _folderPicker.PickFolder(SelectedFolder);
            if (targetFolder is null)
            {
                AddLog("縮放已取消：未選擇目標資料夾。");
                return;
            }

            options = options with { TargetFolderPath = targetFolder };
            var plannedItems = await _resizePreviewService.BuildPreviewAsync(Files.ToList(), options);
            var plannedPreview = new ResizePreviewViewModel(plannedItems);
            CurrentPreview = plannedPreview;

            if (plannedPreview.HasErrors)
            {
                AddLog("縮放已停止：目標資料夾存在衝突或路徑錯誤。");
                RefreshCommands();
                return;
            }
        }

        var snapshot = CurrentPreview is ResizePreviewViewModel latestPreview
            ? latestPreview.Items
            : previewVm.Items;
        var total = snapshot.Count(item => item.IsIncluded && item.ActionKind == OperationActionKind.Resize && !item.HasError);

        IsResizing = true;
        ResizeProgressCurrent = 0;
        ResizeProgressTotal = total > 0 ? total : 1;
        ResizeProgressText = $"準備縮放 {total} 個檔案…";

        _resizeCts = new CancellationTokenSource();

        var progress = new Progress<ResizeProgressReport>(report =>
        {
            ResizeProgressCurrent = report.Current;
            ResizeProgressTotal = report.Total;
            ResizeProgressText = $"{report.Current} / {report.Total}　{report.CurrentFileName}";
        });

        try
        {
            var result = await _resizeExecutor.ExecuteAsync(snapshot, options, progress, _resizeCts.Token);
            if (result.Canceled)
            {
                AddLog($"縮放已由使用者取消：成功 {result.SuccessCount} 個檔案，略過 {result.SkippedCount} 個。");
            }
            else
            {
                AddLog($"縮放執行完成：成功 {result.SuccessCount} 個檔案，略過 {result.SkippedCount} 個。");
            }

            AddErrors(result);
        }
        catch (OperationCanceledException)
        {
            AddLog("縮放已由使用者取消。");
        }
        finally
        {
            IsResizing = false;
            ResizeProgressCurrent = 0;
            ResizeProgressText = string.Empty;
            _resizeCts.Dispose();
            _resizeCts = null;
        }

        RefreshScanFilesCore(keepExclusions: true);
    }

    [RelayCommand(CanExecute = nameof(CanCancelResize))]
    private void CancelResize()
    {
        _resizeCts?.Cancel();
        AddLog("正在取消縮放…");
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

    [ObservableProperty]
    private bool _isLogExpanded = false;

    [RelayCommand]
    private void ToggleLog() => IsLogExpanded = !IsLogExpanded;

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
        InvalidateAnyPreview();
    }

    partial void OnIncludePngChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeJpgChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeJpegChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeWebpChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeBmpChanged(bool value) => InvalidateAnyPreview();

    partial void OnIncludeSubfoldersChanged(bool value) => InvalidateAnyPreview();

    partial void OnFrameDeleteIntervalChanged(int value) => InvalidatePreviewFor(PreviewTool.FrameDelete);

    partial void OnFrameDeleteOutputModeChanged(FrameDeleteOutputMode value) => InvalidatePreviewFor(PreviewTool.FrameDelete);

    partial void OnResizeModeChanged(ResizeMode value) => InvalidatePreviewFor(PreviewTool.Resize);

    partial void OnScaleFactorChanged(double value) => InvalidatePreviewFor(PreviewTool.Resize);

    partial void OnTargetWidthChanged(int value) => InvalidatePreviewFor(PreviewTool.Resize);

    partial void OnTargetHeightChanged(int value) => InvalidatePreviewFor(PreviewTool.Resize);

    partial void OnKeepAspectRatioChanged(bool value) => InvalidatePreviewFor(PreviewTool.Resize);

    partial void OnResizeOutputModeChanged(ResizeOutputMode value) => InvalidatePreviewFor(PreviewTool.Resize);

    partial void OnSelectedResamplerChanged(ResamplerType value) => InvalidatePreviewFor(PreviewTool.Resize);

    partial void OnRenamePrefixChanged(string value) => InvalidatePreviewFor(PreviewTool.Rename);

    partial void OnRenameStartIndexChanged(int value) => InvalidatePreviewFor(PreviewTool.Rename);

    partial void OnRenamePaddingChanged(int value) => InvalidatePreviewFor(PreviewTool.Rename);

    partial void OnRenameOutputModeChanged(RenameOutputMode value) => InvalidatePreviewFor(PreviewTool.Rename);

    partial void OnSelectedToolIndexChanged(int value)
    {
        if (_resizePreviewCts is not null && (PreviewTool)value != PreviewTool.Resize)
        {
            CancelResizePreviewRequest();
        }

        if (CurrentPreview is not null && GetPreviewTool(CurrentPreview) != (PreviewTool)value)
        {
            if (GetPreviewTool(CurrentPreview) == PreviewTool.Resize)
            {
                CancelResizePreviewRequest();
            }

            ClearCurrentPreview();
        }

        TriggerLivePreviewForCurrentTool();
    }

    // ── 私有輔助方法 ──────────────────────────────────────────

    /// <summary>使用者變更共用輸入時，任何既有預覽都不再可信。</summary>
    private void InvalidateAnyPreview()
    {
        CancelResizePreviewRequest();
        ClearCurrentPreview();
    }

    /// <summary>使用者變更特定工具設定時，清除該工具的既有預覽並即時重新產生。</summary>
    private void InvalidatePreviewFor(PreviewTool tool)
    {
        if (tool == PreviewTool.Resize)
        {
            CancelResizePreviewRequest();
        }

        if (CurrentPreview is not null && GetPreviewTool(CurrentPreview) == tool)
        {
            ClearCurrentPreview();
        }

        if ((PreviewTool)SelectedToolIndex == tool)
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
        if (IsResizing || Files.Count == 0)
            return;

        switch ((PreviewTool)SelectedToolIndex)
        {
            case PreviewTool.FrameDelete:
                TriggerFrameDeletePreview();
                break;
            case PreviewTool.Rename:
                TriggerRenamePreview();
                break;
            case PreviewTool.Resize:
                TriggerResizePreviewDebounced();
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

    private void CancelResizePreviewRequest(bool clearBusyState = true)
    {
        if (_resizePreviewCts is null)
        {
            return;
        }

        _resizePreviewCts.Cancel();
        _resizePreviewRequestId++;

        if (clearBusyState)
        {
            IsPreparingPreview = false;
            PreviewBusyText = string.Empty;
            RefreshCommands();
        }
    }

    private static PreviewTool GetPreviewTool(IPreviewViewModel preview) => preview switch
    {
        FrameDeletePreviewViewModel => PreviewTool.FrameDelete,
        RenamePreviewViewModel => PreviewTool.Rename,
        ResizePreviewViewModel => PreviewTool.Resize,
        _ => throw new InvalidOperationException("未知的預覽型別。"),
    };

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

    private IReadOnlySet<string> GetExistingTargetPaths(
        IReadOnlyList<FileItem> files,
        string targetFolderPath,
        Func<FileItem, string> targetNameSelector)
    {
        if (string.IsNullOrWhiteSpace(targetFolderPath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var targetPaths = files.Select(file => Path.Combine(targetFolderPath, targetNameSelector(file)));
        return _fileExistenceService.GetExistingPaths(targetPaths);
    }

    private IReadOnlySet<string> GetExistingRenameTargetPaths(IReadOnlyList<FileItem> files, string targetFolderPath = "")
    {
        if (string.IsNullOrWhiteSpace(targetFolderPath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var folderCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var targetPaths = files.Select(file =>
        {
            folderCounters.TryGetValue(file.DirectoryPath, out var folderIndex);
            folderCounters[file.DirectoryPath] = folderIndex + 1;

            var number = RenameStartIndex + folderIndex;
            var numberText = RenamePadding > 0
                ? number.ToString().PadLeft(RenamePadding, '0')
                : number.ToString();

            return Path.Combine(targetFolderPath, $"{RenamePrefix}{numberText}{file.Extension}");
        });

        return _fileExistenceService.GetExistingPaths(targetPaths);
    }

    private bool HasFiles() => Files.Count > 0;

    private bool CanBrowseOrScan() => !IsResizing && !IsPreparingPreview;

    private bool HasExecutableDeletePreview() =>
        !IsResizing &&
        CurrentPreview is FrameDeletePreviewViewModel vm &&
        vm.HasExecutableItems &&
        !vm.HasErrors;

    private bool HasExecutableRenamePreview() =>
        !IsResizing &&
        CurrentPreview is RenamePreviewViewModel vm &&
        vm.HasExecutableItems &&
        !vm.HasErrors;

    private bool HasExecutableResizePreview() =>
        !IsResizing &&
        CurrentPreview is ResizePreviewViewModel vm &&
        vm.HasExecutableItems &&
        !vm.HasErrors;

    private bool CanCancelResize() => IsResizing;

    private bool CanGoToDownloadPage() =>
        IsUpdateAvailable && !string.IsNullOrWhiteSpace(LatestReleaseUrl);

    private void StartUpdateCheck() =>
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

    /// <summary>從 ViewModel 目前的縮放設定建立 ResizeOptions。</summary>
    private ResizeOptions BuildResizeOptions() =>
        new(
            Mode: ResizeMode,
            ScaleFactor: ScaleFactor,
            TargetWidth: TargetWidth,
            TargetHeight: TargetHeight,
            KeepAspectRatio: KeepAspectRatio,
            OutputMode: ResizeOutputMode,
            TargetFolderPath: string.Empty,
            Resampler: SelectedResampler);

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
    /// 明確通知所有帶 CanExecute 的 command 重新評估狀態。
    /// IsResizing / CurrentPreview 相關的 command 已由 [NotifyCanExecuteChangedFor] 自動處理，
    /// 此處補足掃描結果異動後需要重新評估的指令。
    /// </summary>
    private void RefreshCommands()
    {
        BrowseFolderCommand.NotifyCanExecuteChanged();
        ScanFilesCommand.NotifyCanExecuteChanged();
        ImportDroppedPathsCommand.NotifyCanExecuteChanged();
        ExecuteFrameDeleteCommand.NotifyCanExecuteChanged();
        ExecuteRenameCommand.NotifyCanExecuteChanged();
        ExecuteResizeCommand.NotifyCanExecuteChanged();
        CancelResizeCommand.NotifyCanExecuteChanged();
    }
}
