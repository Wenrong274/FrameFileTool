using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IFileScanner _scanner;
    private readonly IFrameDeletePlanner _frameDeletePlanner;
    private readonly IRenamePlanner _renamePlanner;
    private readonly IFileOperationExecutor _executor;
    private readonly IFolderPickerService _folderPicker;

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

    [ObservableProperty]
    private int _frameDeleteInterval = 3;

    [ObservableProperty]
    private string _renamePrefix = "F_";

    [ObservableProperty]
    private int _renameStartIndex;

    [ObservableProperty]
    private int _renamePadding;

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

    public ObservableCollection<FileItem> Files { get; } = new();
    public ObservableCollection<OperationPreviewItem> PreviewItems { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();

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

        var deleteCount = PreviewItems.Count(item => item.Action == "刪除" && !item.HasError);
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

        var preview = _renamePlanner.Plan(Files.ToList(), RenamePrefix, RenameStartIndex, RenamePadding);
        foreach (var item in preview)
        {
            PreviewItems.Add(item);
        }

        var renameCount = PreviewItems.Count(item => item.Action == "改名" && !item.HasError);
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

    private IReadOnlyList<string> GetSelectedExtensions()
    {
        var extensions = new List<string>();

        if (IncludePng)   extensions.Add(".png");
        if (IncludeJpg)   extensions.Add(".jpg");
        if (IncludeJpeg)  extensions.Add(".jpeg");
        if (IncludeWebp)  extensions.Add(".webp");
        if (IncludeBmp)   extensions.Add(".bmp");

        return extensions;
    }

    private bool HasFiles() => Files.Count > 0;

    private bool HasExecutableDeletePreview() =>
        PreviewItems.Any(item => item.Action == "刪除" && !item.HasError);

    private bool HasExecutableRenamePreview() =>
        PreviewItems.Any(item => item.Action == "改名" && !item.HasError) &&
        PreviewItems.All(item => !item.HasError);

    private void AddLog(string message) =>
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");

    private void AddErrors(OperationResult result)
    {
        foreach (var error in result.Errors)
        {
            AddLog($"錯誤：{error}");
        }
    }

    private void RefreshCommands()
    {
        PreviewFrameDeleteCommand.NotifyCanExecuteChanged();
        ExecuteFrameDeleteCommand.NotifyCanExecuteChanged();
        PreviewRenameCommand.NotifyCanExecuteChanged();
        ExecuteRenameCommand.NotifyCanExecuteChanged();
    }
}
