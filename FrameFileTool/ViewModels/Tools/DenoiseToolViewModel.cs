using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;

namespace FrameFileTool.ViewModels.Tools;

/// <summary>
/// 批次降噪工具：持有降噪模式設定，管理清單預覽、執行進度、取消流程與降噪局部比較預覽。
/// 批次降噪固定覆寫原檔，不提供輸出位置選擇。
/// 共用狀態與執行流程透過 <see cref="IToolContext"/> 協調。
/// </summary>
public sealed partial class DenoiseToolViewModel : ObservableObject, IDisposable
{
    private readonly IDenoisePlanner _planner;
    private readonly IDenoiseExecutor _executor;
    private readonly IDenoisePreviewService _denoisePreviewService;
    private readonly IToolContext _context;

    private CancellationTokenSource? _denoiseCts;
    private CancellationTokenSource? _denoisePreviewCts;

    // ── 降噪設定 ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedModeHint))]
    private DenoiseMode _selectedMode = DenoiseMode.Standard;

    /// <summary>
    /// 降噪模式選項清單，供 ComboBox 繫結。
    /// 不含 <see cref="DenoiseMode.Off"/>：獨立工具中「不降噪」沒有意義。
    /// </summary>
    public IReadOnlyList<DenoiseMode> ModeOptions { get; } =
        [DenoiseMode.Detail, DenoiseMode.Standard, DenoiseMode.Strong];

    /// <summary>依選取的降噪模式顯示對應的使用建議說明，供 UI HintText 繫結。</summary>
    public string SelectedModeHint => SelectedMode switch
    {
        DenoiseMode.Detail => "輕度 Wavelet 降噪，保留邊緣與細節",
        DenoiseMode.Standard => "中度降噪，平衡顆粒消除與細節保留",
        DenoiseMode.Strong => "強力降噪，顆粒明顯降低；影像可能偏柔和，建議先放大比較確認",
        _ => string.Empty,
    };

    // ── 執行進度狀態 ─────────────────────────────────────────

    /// <summary>
    /// 降噪正在執行中。為 true 時所有工具的非取消操作都應停用，
    /// 變更時透過 context 通知全部命令重新評估。
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExecuteCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isDenoising;

    /// <summary>目前已完成的降噪筆數，供進度條的 Value 繫結。</summary>
    [ObservableProperty]
    private int _progressCurrent;

    /// <summary>本次降噪的總筆數，供進度條的 Maximum 繫結。</summary>
    [ObservableProperty]
    private int _progressTotal = 1;

    /// <summary>進度條下方的文字描述，例如「3 / 10  frame003.png」。</summary>
    [ObservableProperty]
    private string _progressText = string.Empty;

    // ── 降噪局部預覽狀態 ─────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateDenoisePreviewCommand))]
    private bool _isGeneratingDenoisePreview;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDenoisePreviewError))]
    private string _denoisePreviewErrorMessage = string.Empty;

    public bool HasDenoisePreviewError => !string.IsNullOrEmpty(DenoisePreviewErrorMessage);

    /// <summary>降噪預覽產生成功時觸發，供 View 自動開啟比較視窗。</summary>
    public event Action<DenoisePreviewResult>? DenoisePreviewGenerated;

    internal DenoiseToolViewModel(
        IDenoisePlanner planner,
        IDenoiseExecutor executor,
        IDenoisePreviewService denoisePreviewService,
        IToolContext context)
    {
        _planner = planner;
        _executor = executor;
        _denoisePreviewService = denoisePreviewService;
        _context = context;
    }

    // ── 設定變更 → 預覽失效 ──────────────────────────────────

    partial void OnSelectedModeChanged(DenoiseMode value) =>
        _context.NotifySettingChanged(PreviewTool.Denoise);

    partial void OnIsDenoisingChanged(bool value) => _context.RefreshCommands();

    // ── 清單預覽 ─────────────────────────────────────────────

    /// <summary>依目前設定產生降噪預覽並設為當前預覽。planner 為 pure function，同步計算。</summary>
    internal void TriggerPreview()
    {
        var files = _context.SnapshotFiles();
        var items = _planner.Plan(files, new DenoiseOptions(SelectedMode));
        if (items is null)
            return;

        var denoiseCount = items.Count(i => i.ActionKind == OperationActionKind.Denoise && !i.HasError);
        var vm = new DenoisePreviewViewModel(items);
        _context.SetCurrentPreview(
            vm,
            $"降噪預覽完成：預計降噪 {denoiseCount} 個檔案（覆寫原檔）。");
    }

    // ── 執行與取消 ───────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(HasExecutablePreview))]
    private async Task Execute()
    {
        if (_context.CurrentPreview is not DenoisePreviewViewModel previewVm)
            return;

        _context.AddLog("⚠ 批次降噪：原始圖片將被降噪結果取代，無法還原。");

        var snapshot = previewVm.Items;
        var total = snapshot.Count(item => item.IsIncluded && item.ActionKind == OperationActionKind.Denoise && !item.HasError);

        IsDenoising = true;
        ProgressCurrent = 0;
        ProgressTotal = total > 0 ? total : 1;
        ProgressText = $"準備降噪 {total} 個檔案…";

        _denoiseCts = new CancellationTokenSource();

        var progress = new Progress<ResizeProgressReport>(report =>
        {
            ProgressCurrent = report.Current;
            ProgressTotal = report.Total;
            ProgressText = $"{report.Current} / {report.Total}　{report.CurrentFileName}";
        });

        try
        {
            var result = await _executor.ExecuteAsync(
                snapshot, new DenoiseOptions(SelectedMode), progress, _denoiseCts.Token);
            if (result.Canceled)
            {
                _context.AddLog($"降噪已由使用者取消：成功 {result.SuccessCount} 個檔案，略過 {result.SkippedCount} 個。");
            }
            else
            {
                _context.AddLog($"降噪執行完成：成功 {result.SuccessCount} 個檔案，略過 {result.SkippedCount} 個。");
            }

            _context.AddErrors(result);
        }
        catch (OperationCanceledException)
        {
            _context.AddLog("降噪已由使用者取消。");
        }
        finally
        {
            IsDenoising = false;
            ProgressCurrent = 0;
            ProgressText = string.Empty;
            _denoiseCts.Dispose();
            _denoiseCts = null;
        }

        _context.RescanKeepingExclusions();
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _denoiseCts?.Cancel();
        _context.AddLog("正在取消降噪…");
    }

    private bool HasExecutablePreview() =>
        !_context.IsBatchExecuting &&
        _context.CurrentPreview is DenoisePreviewViewModel vm &&
        vm.HasExecutableItems &&
        !vm.HasErrors;

    private bool CanCancel() => IsDenoising;

    // ── 降噪局部預覽（放大比較） ─────────────────────────────

    [RelayCommand(CanExecute = nameof(CanGenerateDenoisePreview))]
    private async Task GenerateDenoisePreview()
    {
        var sourceFile = _context.SnapshotFiles().FirstOrDefault();
        if (sourceFile is null)
            return;

        _denoisePreviewCts?.Cancel();
        _denoisePreviewCts?.Dispose();
        _denoisePreviewCts = new CancellationTokenSource();
        var cts = _denoisePreviewCts;

        IsGeneratingDenoisePreview = true;
        DenoisePreviewErrorMessage = string.Empty;

        try
        {
            var modes = new[] { DenoiseMode.Detail, DenoiseMode.Standard, DenoiseMode.Strong };
            var result = await _denoisePreviewService.GeneratePreviewAsync(
                sourceFile.FullPath, modes, cts.Token);

            if (cts.IsCancellationRequested)
                return;

            if (result.HasError)
            {
                DenoisePreviewErrorMessage = result.ErrorMessage!;
            }
            else
            {
                DenoisePreviewGenerated?.Invoke(result);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (!cts.IsCancellationRequested)
                IsGeneratingDenoisePreview = false;
        }
    }

    private bool CanGenerateDenoisePreview() =>
        !IsGeneratingDenoisePreview && _context.HasFiles;

    // ── 輔助 ─────────────────────────────────────────────────

    /// <summary>通知本工具的命令重新評估 CanExecute。</summary>
    internal void RefreshCommands()
    {
        ExecuteCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        GenerateDenoisePreviewCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _denoisePreviewCts?.Cancel();
        _denoisePreviewCts?.Dispose();
    }
}
