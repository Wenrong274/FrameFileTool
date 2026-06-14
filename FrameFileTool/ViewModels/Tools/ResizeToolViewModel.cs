using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;

namespace FrameFileTool.ViewModels.Tools;

/// <summary>
/// 批次縮放工具：持有縮放與演算法設定，
/// 管理防抖即時預覽、執行進度與取消流程。
/// 共用狀態與執行流程透過 <see cref="IToolContext"/> 協調。
/// </summary>
public sealed partial class ResizeToolViewModel : ObservableObject
{
    private readonly IImageResizeExecutor _resizeExecutor;
    private readonly IResizePreviewService _resizePreviewService;
    private readonly IOutputFolderResolver _outputFolderResolver;
    private readonly IToolContext _context;
    private readonly TimeSpan _debounceDelay;

    private CancellationTokenSource? _resizeCts;
    private CancellationTokenSource? _resizePreviewCts;
    private long _resizePreviewRequestId;

    // ── 縮放設定 ──────────────────────────────────────────────

    [ObservableProperty]
    private ResizeMode _mode = ResizeMode.ScaleFactor;

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
    private ResizeOutputMode _outputMode = ResizeOutputMode.TargetFolder;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResamplerHint))]
    private ResamplerType _selectedResampler = ResamplerType.Bicubic;

    /// <summary>縮放演算法選項清單，供 ComboBox 繫結。</summary>
    public IReadOnlyList<ResamplerType> ResamplerOptions { get; } =
        Enum.GetValues<ResamplerType>().ToList();

    /// <summary>依選取的演算法顯示對應的使用建議說明，供 UI HintText 繫結。</summary>
    public string ResamplerHint => SelectedResampler switch
    {
        ResamplerType.Lanczos3 => "大幅縮小時保持文字與線條清晰",
        ResamplerType.CatmullRom => "放大時邊緣比一般用途更銳利",
        ResamplerType.NearestNeighbor => "整數倍縮放截圖，保持像素對齊",
        ResamplerType.MitchellNetravali => "線條圖需要最銳利邊緣時使用",
        _ => "大多數縮放情境的穩定選擇",
    };

    // ── 執行進度狀態 ─────────────────────────────────────────

    /// <summary>
    /// 縮放正在執行中。為 true 時所有工具的非取消操作都應停用，
    /// 變更時透過 context 通知全部命令重新評估。
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isResizing;

    /// <summary>目前已完成的縮放筆數，供進度條的 Value 繫結。</summary>
    [ObservableProperty]
    private int _progressCurrent;

    /// <summary>本次縮放的總筆數，供進度條的 Maximum 繫結。</summary>
    [ObservableProperty]
    private int _progressTotal = 1;

    /// <summary>進度條下方的文字描述，例如「3 / 10  frame003.png」。</summary>
    [ObservableProperty]
    private string _progressText = string.Empty;

    /// <summary>正在執行的即時預覽 Task，供測試層 await 結果。</summary>
    internal Task LivePreviewTask { get; private set; } = Task.CompletedTask;

    internal ResizeToolViewModel(
        IImageResizeExecutor resizeExecutor,
        IResizePreviewService resizePreviewService,
        IOutputFolderResolver outputFolderResolver,
        IToolContext context,
        TimeSpan debounceDelay)
    {
        _resizeExecutor = resizeExecutor;
        _resizePreviewService = resizePreviewService;
        _outputFolderResolver = outputFolderResolver;
        _context = context;
        _debounceDelay = debounceDelay;
    }

    // ── 設定變更 → 預覽失效 ──────────────────────────────────

    partial void OnModeChanged(ResizeMode value) => NotifyResizeSettingChanged();

    partial void OnScaleFactorChanged(double value) => NotifyResizeSettingChanged();

    partial void OnTargetWidthChanged(int value) => NotifyResizeSettingChanged();

    partial void OnTargetHeightChanged(int value) => NotifyResizeSettingChanged();

    partial void OnKeepAspectRatioChanged(bool value) => NotifyResizeSettingChanged();

    partial void OnOutputModeChanged(ResizeOutputMode value) => NotifyResizeSettingChanged();

    partial void OnSelectedResamplerChanged(ResamplerType value) => NotifyResizeSettingChanged();

    partial void OnIsResizingChanged(bool value) => _context.RefreshCommands();

    private void NotifyResizeSettingChanged() =>
        _context.NotifySettingChanged(PreviewTool.Resize);

    // ── 防抖即時預覽 ─────────────────────────────────────────

    /// <summary>以 debounce 觸發非同步縮放預覽，連續變更只會執行最後一次。</summary>
    internal void TriggerPreviewDebounced()
    {
        CancelPendingPreview(clearBusyState: false);
        var cts = new CancellationTokenSource();
        _resizePreviewCts = cts;
        var requestId = ++_resizePreviewRequestId;
        LivePreviewTask = RunDelayedPreviewAsync(requestId, cts);
    }

    /// <summary>取消尚未完成的預覽請求；預設同時清除忙碌狀態。</summary>
    internal void CancelPendingPreview(bool clearBusyState = true)
    {
        if (_resizePreviewCts is null)
        {
            return;
        }

        _resizePreviewCts.Cancel();
        _resizePreviewRequestId++;

        if (clearBusyState)
        {
            _context.IsPreparingPreview = false;
            _context.PreviewBusyText = string.Empty;
            _context.RefreshCommands();
        }
    }

    private async Task RunDelayedPreviewAsync(long requestId, CancellationTokenSource previewCts)
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

        await RunPreviewCoreAsync(requestId, previewCts);
    }

    private async Task RunPreviewCoreAsync(long requestId, CancellationTokenSource previewCts)
    {
        var options = BuildResizeOptions();
        var files = _context.SnapshotFiles();

        _context.CurrentPreview = null;
        _context.PreviewBusyText = "正在讀取圖片尺寸…";
        _context.IsPreparingPreview = true;

        try
        {
            var items = await _resizePreviewService.BuildPreviewAsync(files, options, previewCts.Token);
            if (previewCts.IsCancellationRequested || requestId != _resizePreviewRequestId)
            {
                return;
            }

            var resizeCount = items.Count(i => i.ActionKind == OperationActionKind.Resize && !i.HasError);
            var resizeErrorCount = items.Count(i => i.HasError);
            var vm = new ResizePreviewViewModel(items);
            _context.SetCurrentPreview(
                vm,
                $"縮放預覽完成：預計縮放 {resizeCount} 個檔案，錯誤 {resizeErrorCount} 個。");
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
                _context.IsPreparingPreview = false;
                _context.PreviewBusyText = string.Empty;
                _context.RefreshCommands();
            }
        }
    }

    // ── 執行與取消 ───────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(HasExecutablePreview))]
    private async Task Execute()
    {
        if (_context.CurrentPreview is not ResizePreviewViewModel previewVm)
            return;

        var options = BuildResizeOptions();

        if (options.OutputMode == ResizeOutputMode.Overwrite)
        {
            _context.AddLog("⚠ 覆寫模式：原始圖片將被取代，無法還原。");
        }
        else if (options.OutputMode == ResizeOutputMode.TargetFolder)
        {
            var targetFolder = _context.PickTargetFolderOrLogCancel("縮放");
            if (targetFolder is null)
            {
                return;
            }

            var resolvedFolder = _outputFolderResolver.ResolveForResize(_context.SelectedFolder, targetFolder, options);
            if (resolvedFolder.WasAutoRedirected && !string.IsNullOrWhiteSpace(resolvedFolder.LogMessage))
            {
                _context.AddLog(resolvedFolder.LogMessage);
            }

            options = options with { TargetFolderPath = resolvedFolder.TargetFolderPath };
            var plannedItems = await _resizePreviewService.BuildPreviewAsync(_context.SnapshotFiles(), options);
            if (!_context.ApplyPlannedPreviewOrLogConflict("縮放", new ResizePreviewViewModel(plannedItems)))
            {
                return;
            }
        }

        var snapshot = _context.CurrentPreview is ResizePreviewViewModel latestPreview
            ? latestPreview.Items
            : previewVm.Items;
        var total = snapshot.Count(item => item.IsIncluded && item.ActionKind == OperationActionKind.Resize && !item.HasError);

        IsResizing = true;
        ProgressCurrent = 0;
        ProgressTotal = total > 0 ? total : 1;
        ProgressText = $"準備縮放 {total} 個檔案…";

        _resizeCts = new CancellationTokenSource();

        var progress = new Progress<ResizeProgressReport>(report =>
        {
            ProgressCurrent = report.Current;
            ProgressTotal = report.Total;
            ProgressText = $"{report.Current} / {report.Total}　{report.CurrentFileName}";
        });

        try
        {
            var result = await _resizeExecutor.ExecuteAsync(snapshot, options, progress, _resizeCts.Token);
            if (result.Canceled)
            {
                _context.AddLog($"縮放已由使用者取消：成功 {result.SuccessCount} 個檔案，略過 {result.SkippedCount} 個。");
            }
            else
            {
                _context.AddLog($"縮放執行完成：成功 {result.SuccessCount} 個檔案，略過 {result.SkippedCount} 個。");
            }

            _context.AddErrors(result);
        }
        catch (OperationCanceledException)
        {
            _context.AddLog("縮放已由使用者取消。");
        }
        finally
        {
            IsResizing = false;
            ProgressCurrent = 0;
            ProgressText = string.Empty;
            _resizeCts.Dispose();
            _resizeCts = null;
        }

        _context.RescanKeepingExclusions();
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _resizeCts?.Cancel();
        _context.AddLog("正在取消縮放…");
    }

    private bool HasExecutablePreview() =>
        !_context.IsBatchExecuting &&
        _context.CurrentPreview is ResizePreviewViewModel vm &&
        vm.HasExecutableItems &&
        !vm.HasErrors;

    private bool CanCancel() => IsResizing;

    // ── 輔助 ─────────────────────────────────────────────────

    /// <summary>從目前的縮放設定建立 ResizeOptions。</summary>
    private ResizeOptions BuildResizeOptions() =>
        new(
            Mode: Mode,
            ScaleFactor: ScaleFactor,
            TargetWidth: TargetWidth,
            TargetHeight: TargetHeight,
            KeepAspectRatio: KeepAspectRatio,
            OutputMode: OutputMode,
            TargetFolderPath: string.Empty,
            Resampler: SelectedResampler);

    /// <summary>通知本工具的命令重新評估 CanExecute。</summary>
    internal void RefreshCommands()
    {
        ExecuteCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }
}
