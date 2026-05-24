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
    private int _frameDeleteInterval = 3;

    // ── 改名設定 ──────────────────────────────────────────────

    [ObservableProperty]
    private string _renamePrefix = "F_";

    [ObservableProperty]
    private int _renameStartIndex;

    [ObservableProperty]
    private int _renamePadding;

    // ── UI 狀態 ───────────────────────────────────────────────

    [ObservableProperty]
    private int _selectedToolIndex;

    [ObservableProperty]
    private string _fileSummary = "尚未掃描";

    public MainViewModel(
        IFileScanner scanner,
        IFrameDeletePlanner frameDeletePlanner,
        IRenamePlanner renamePlanner,
        IFileOperationExecutor executor,
        IFolderPickerService folderPicker)
    {
        _scanner = scanner;
        _frameDeletePlanner = frameDeletePlanner;
        _renamePlanner = renamePlanner;
        _executor = executor;
        _folderPicker = folderPicker;
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

    [RelayCommand]
    private void ClearLog() => Logs.Clear();

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
    }
}
