using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.ViewModels;

/// <summary>
/// 主視窗的 ViewModel，管理 UI 狀態與 command 生命週期。
/// 所有商業邏輯均委派給對應的 service；ViewModel 只負責
/// 狀態同步、log 記錄與 command 的 CanExecute 管理。
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IFileScanner _scanner;
    private readonly IFrameDeletePlanner _frameDeletePlanner;
    private readonly IRenamePlanner _renamePlanner;
    private readonly IFileOperationExecutor _executor;
    private readonly IFolderPickerService _folderPicker;
    private readonly IResizePlanner _resizePlanner;
    private readonly IImageResizeExecutor _resizeExecutor;

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

    // ── 縮放設定 ──────────────────────────────────────────────

    [ObservableProperty]
    private ResizeMode _resizeMode = ResizeMode.Percentage;

    [ObservableProperty]
    private int _scalePercent = 50;

    [ObservableProperty]
    private int _targetWidth;

    [ObservableProperty]
    private int _targetHeight;

    [ObservableProperty]
    private bool _keepAspectRatio = true;

    [ObservableProperty]
    private ResizeOutputMode _resizeOutputMode = ResizeOutputMode.Subfolder;

    [ObservableProperty]
    private string _resizeSubfolderName = "resized";

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

    // ── UI 狀態 ───────────────────────────────────────────────

    [ObservableProperty]
    private int _selectedToolIndex;

    [ObservableProperty]
    private string _fileSummary = "尚未掃描";

    /// <summary>預覽摘要，顯示於預覽表格上方，說明計畫筆數與錯誤數。</summary>
    [ObservableProperty]
    private string _previewSummary = "預覽修改";

    /// <summary>是否已產生預覽（true 代表預覽摘要列應顯示顏色狀態）。</summary>
    [ObservableProperty]
    private bool _hasPreview;

    /// <summary>當前預覽是否含有錯誤項目，供摘要列切換紅色警示。</summary>
    [ObservableProperty]
    private bool _hasPreviewErrors;

    /// <summary>
    /// 依選取的演算法顯示對應的使用建議說明，供 UI HintText 繫結。
    /// </summary>
    public string ResamplerHint => SelectedResampler switch
    {
        ResamplerType.Lanczos3          => "大幅縮小時保持文字與線條清晰",
        ResamplerType.CatmullRom        => "放大時邊緣比一般用途更銳利",
        ResamplerType.NearestNeighbor   => "整數倍縮放截圖，保持像素對齊",
        ResamplerType.MitchellNetravali => "線條圖需要最銳利邊緣時使用",
        _                               => "大多數縮放情境的穩定選擇",
    };

    public MainViewModel(
        IFileScanner scanner,
        IFrameDeletePlanner frameDeletePlanner,
        IRenamePlanner renamePlanner,
        IFileOperationExecutor executor,
        IFolderPickerService folderPicker,
        IResizePlanner resizePlanner,
        IImageResizeExecutor resizeExecutor)
    {
        _scanner = scanner;
        _frameDeletePlanner = frameDeletePlanner;
        _renamePlanner = renamePlanner;
        _executor = executor;
        _folderPicker = folderPicker;
        _resizePlanner = resizePlanner;
        _resizeExecutor = resizeExecutor;
    }

    /// <summary>掃描結果檔案清單，繫結到 DataGrid。</summary>
    public ObservableCollection<FileItem> Files { get; } = [];

    /// <summary>操作預覽清單，繫結到預覽 DataGrid。</summary>
    public ObservableCollection<OperationPreviewItem> PreviewItems { get; } = [];

    /// <summary>操作 log，最新訊息在最上方。</summary>
    public ObservableCollection<string> Logs { get; } = [];

    // ── Commands ──────────────────────────────────────────────

    [RelayCommand]
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

    [RelayCommand]
    private void ScanFiles()
    {
        Files.Clear();
        PreviewItems.Clear();
        PreviewSummary = "選擇操作並點擊預覽";
        HasPreview = false;
        HasPreviewErrors = false;

        var extensions = GetSelectedExtensions();
        var files = _scanner.Scan(SelectedFolder, extensions, IncludeSubfolders);

        foreach (var file in files)
        {
            Files.Add(file);
        }

        FileSummary = $"已掃描 {Files.Count} 個圖片檔";
        AddLog($"{FileSummary}。資料夾：{SelectedFolder}");
        RefreshCommands();
    }

    [RelayCommand(CanExecute = nameof(HasFiles))]
    private void PreviewFrameDelete()
    {
        PreviewItems.Clear();

        var preview = _frameDeletePlanner.Plan(Files.ToList(), FrameDeleteInterval);
        foreach (var item in preview)
        {
            PreviewItems.Add(item);
        }

        var deleteCount = PreviewItems.Count(item =>
            item.Action == OperationAction.Delete && !item.HasError);
        var errorCount = PreviewItems.Count(item => item.HasError);

        HasPreview = true;
        HasPreviewErrors = errorCount > 0;
        PreviewSummary = errorCount > 0
            ? $"共 {PreviewItems.Count} 個項目，預計刪除 {deleteCount} 個，{errorCount} 個錯誤"
            : $"共 {PreviewItems.Count} 個項目，預計刪除 {deleteCount} 個";

        AddLog($"抽幀預覽完成：每 {FrameDeleteInterval} 張刪除 1 張，預計刪除 {deleteCount} 個檔案。");
        RefreshCommands();
    }

    [RelayCommand(CanExecute = nameof(HasExecutableDeletePreview))]
    private void ExecuteFrameDelete()
    {
        var result = _executor.DeleteToRecycleBin(PreviewItems);
        AddLog($"抽幀執行完成：已移到回收桶 {result.SuccessCount} 個檔案。");
        AddErrors(result);
        ScanFiles();
    }

    [RelayCommand(CanExecute = nameof(HasFiles))]
    private void PreviewRename()
    {
        PreviewItems.Clear();

        var preview = _renamePlanner.Plan(
            Files.ToList(), RenamePrefix, RenameStartIndex, RenamePadding);

        foreach (var item in preview)
        {
            PreviewItems.Add(item);
        }

        var renameCount = PreviewItems.Count(item =>
            item.Action == OperationAction.Rename && !item.HasError);
        var errorCount = PreviewItems.Count(item => item.HasError);

        HasPreview = true;
        HasPreviewErrors = errorCount > 0;
        PreviewSummary = errorCount > 0
            ? $"共 {PreviewItems.Count} 個項目，預計改名 {renameCount} 個，{errorCount} 個錯誤（執行已停用）"
            : $"共 {PreviewItems.Count} 個項目，預計改名 {renameCount} 個";

        AddLog($"改名預覽完成：預計改名 {renameCount} 個檔案，錯誤 {errorCount} 個。");
        RefreshCommands();
    }

    [RelayCommand(CanExecute = nameof(HasExecutableRenamePreview))]
    private void ExecuteRename()
    {
        var result = _executor.RenameFiles(PreviewItems);
        AddLog($"改名執行完成：成功 {result.SuccessCount} 個檔案。");
        AddErrors(result);
        ScanFiles();
    }

    [RelayCommand(CanExecute = nameof(HasFiles))]
    private void PreviewResize()
    {
        PreviewItems.Clear();

        var options = BuildResizeOptions();
        var preview = _resizePlanner.Plan(Files.ToList(), options);

        foreach (var item in preview)
        {
            PreviewItems.Add(item);
        }

        var resizeCount = PreviewItems.Count(item =>
            item.Action == OperationAction.Resize && !item.HasError);
        var errorCount = PreviewItems.Count(item => item.HasError);

        HasPreview = true;
        HasPreviewErrors = errorCount > 0;
        PreviewSummary = errorCount > 0
            ? $"共 {PreviewItems.Count} 個項目，預計縮放 {resizeCount} 個，{errorCount} 個錯誤（執行已停用）"
            : $"共 {PreviewItems.Count} 個項目，預計縮放 {resizeCount} 個";

        AddLog($"縮放預覽完成：預計縮放 {resizeCount} 個檔案，錯誤 {errorCount} 個。");
        RefreshCommands();
    }

    [RelayCommand(CanExecute = nameof(HasExecutableResizePreview))]
    private void ExecuteResize()
    {
        var options = BuildResizeOptions();

        if (options.OutputMode == ResizeOutputMode.Overwrite)
        {
            AddLog("⚠ 覆寫模式：原始圖片將被取代，無法還原。");
        }

        var result = _resizeExecutor.Execute(PreviewItems, options);
        AddLog($"縮放執行完成：成功 {result.SuccessCount} 個檔案。");
        AddErrors(result);
        ScanFiles();
    }

    [RelayCommand]
    private void ClearLog() => Logs.Clear();

    [ObservableProperty]
    private bool _isLogExpanded = false;

    [RelayCommand]
    private void ToggleLog() => IsLogExpanded = !IsLogExpanded;

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

    private bool HasFiles() => Files.Count > 0;

    private bool HasExecutableDeletePreview() =>
        PreviewItems.Any(item => item.Action == OperationAction.Delete && !item.HasError);

    private bool HasExecutableRenamePreview() =>
        PreviewItems.Any(item => item.Action == OperationAction.Rename && !item.HasError) &&
        PreviewItems.All(item => !item.HasError);

    private bool HasExecutableResizePreview() =>
        PreviewItems.Any(item => item.Action == OperationAction.Resize && !item.HasError) &&
        PreviewItems.All(item => !item.HasError);

    /// <summary>從 ViewModel 目前的縮放設定建立 ResizeOptions。</summary>
    private ResizeOptions BuildResizeOptions() =>
        new(
            Mode:            ResizeMode,
            ScalePercent:    ScalePercent,
            TargetWidth:     TargetWidth,
            TargetHeight:    TargetHeight,
            KeepAspectRatio: KeepAspectRatio,
            OutputMode:      ResizeOutputMode,
            SubfolderName:   ResizeSubfolderName,
            Resampler:       SelectedResampler);

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

    /// <summary>明確通知所有帶 CanExecute 的 command 重新評估狀態。</summary>
    private void RefreshCommands()
    {
        PreviewFrameDeleteCommand.NotifyCanExecuteChanged();
        ExecuteFrameDeleteCommand.NotifyCanExecuteChanged();
        PreviewRenameCommand.NotifyCanExecuteChanged();
        ExecuteRenameCommand.NotifyCanExecuteChanged();
        PreviewResizeCommand.NotifyCanExecuteChanged();
        ExecuteResizeCommand.NotifyCanExecuteChanged();
    }
}
