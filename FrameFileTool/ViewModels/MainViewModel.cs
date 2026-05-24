using System.Collections.ObjectModel;
using System.Windows.Input;
using FrameFileTool.Commands;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly FileScanner _scanner;
    private readonly FrameDeletePlanner _frameDeletePlanner;
    private readonly RenamePlanner _renamePlanner;
    private readonly FileOperationExecutor _executor;
    private readonly FolderPickerService _folderPicker;

    private string _selectedFolder = string.Empty;
    private bool _includePng = true;
    private bool _includeJpg = true;
    private bool _includeJpeg = true;
    private bool _includeWebp;
    private bool _includeBmp;
    private bool _includeSubfolders;
    private int _frameDeleteInterval = 3;
    private string _renamePrefix = "F_";
    private int _renameStartIndex;
    private int _renamePadding;
    private int _selectedToolIndex;
    private string _fileSummary = "尚未掃描";

    public MainViewModel(
        FileScanner scanner,
        FrameDeletePlanner frameDeletePlanner,
        RenamePlanner renamePlanner,
        FileOperationExecutor executor,
        FolderPickerService folderPicker)
    {
        _scanner = scanner;
        _frameDeletePlanner = frameDeletePlanner;
        _renamePlanner = renamePlanner;
        _executor = executor;
        _folderPicker = folderPicker;

        BrowseFolderCommand = new RelayCommand(BrowseFolder);
        ScanCommand = new RelayCommand(ScanFiles);
        PreviewFrameDeleteCommand = new RelayCommand(PreviewFrameDelete, HasFiles);
        ExecuteFrameDeleteCommand = new RelayCommand(ExecuteFrameDelete, HasExecutableDeletePreview);
        PreviewRenameCommand = new RelayCommand(PreviewRename, HasFiles);
        ExecuteRenameCommand = new RelayCommand(ExecuteRename, HasExecutableRenamePreview);
        ClearLogCommand = new RelayCommand(() => Logs.Clear());
    }

    public ObservableCollection<FileItem> Files { get; } = new();
    public ObservableCollection<OperationPreviewItem> PreviewItems { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();

    public string SelectedFolder
    {
        get => _selectedFolder;
        set => SetProperty(ref _selectedFolder, value);
    }

    public bool IncludePng
    {
        get => _includePng;
        set => SetProperty(ref _includePng, value);
    }

    public bool IncludeJpg
    {
        get => _includeJpg;
        set => SetProperty(ref _includeJpg, value);
    }

    public bool IncludeJpeg
    {
        get => _includeJpeg;
        set => SetProperty(ref _includeJpeg, value);
    }

    public bool IncludeWebp
    {
        get => _includeWebp;
        set => SetProperty(ref _includeWebp, value);
    }

    public bool IncludeBmp
    {
        get => _includeBmp;
        set => SetProperty(ref _includeBmp, value);
    }

    public bool IncludeSubfolders
    {
        get => _includeSubfolders;
        set => SetProperty(ref _includeSubfolders, value);
    }

    public int FrameDeleteInterval
    {
        get => _frameDeleteInterval;
        set => SetProperty(ref _frameDeleteInterval, value);
    }

    public string RenamePrefix
    {
        get => _renamePrefix;
        set => SetProperty(ref _renamePrefix, value);
    }

    public int RenameStartIndex
    {
        get => _renameStartIndex;
        set => SetProperty(ref _renameStartIndex, value);
    }

    public int RenamePadding
    {
        get => _renamePadding;
        set => SetProperty(ref _renamePadding, Math.Max(0, value));
    }

    public int SelectedToolIndex
    {
        get => _selectedToolIndex;
        set => SetProperty(ref _selectedToolIndex, value);
    }

    public string FileSummary
    {
        get => _fileSummary;
        private set => SetProperty(ref _fileSummary, value);
    }

    public ICommand BrowseFolderCommand { get; }
    public ICommand ScanCommand { get; }
    public ICommand PreviewFrameDeleteCommand { get; }
    public ICommand ExecuteFrameDeleteCommand { get; }
    public ICommand PreviewRenameCommand { get; }
    public ICommand ExecuteRenameCommand { get; }
    public ICommand ClearLogCommand { get; }

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

    private void ExecuteFrameDelete()
    {
        var result = _executor.DeleteToRecycleBin(PreviewItems);
        AddLog($"抽幀執行完成：已移到回收桶 {result.SuccessCount} 個檔案。");
        AddErrors(result);
        ScanFiles();
    }

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

    private void ExecuteRename()
    {
        var result = _executor.RenameFiles(PreviewItems);
        AddLog($"改名執行完成：成功 {result.SuccessCount} 個檔案。");
        AddErrors(result);
        ScanFiles();
    }

    private IReadOnlyList<string> GetSelectedExtensions()
    {
        var extensions = new List<string>();

        if (IncludePng)
        {
            extensions.Add(".png");
        }

        if (IncludeJpg)
        {
            extensions.Add(".jpg");
        }

        if (IncludeJpeg)
        {
            extensions.Add(".jpeg");
        }

        if (IncludeWebp)
        {
            extensions.Add(".webp");
        }

        if (IncludeBmp)
        {
            extensions.Add(".bmp");
        }

        return extensions;
    }

    private bool HasFiles()
    {
        return Files.Count > 0;
    }

    private bool HasExecutableDeletePreview()
    {
        return PreviewItems.Any(item => item.Action == "刪除" && !item.HasError);
    }

    private bool HasExecutableRenamePreview()
    {
        return PreviewItems.Any(item => item.Action == "改名" && !item.HasError) &&
               PreviewItems.All(item => !item.HasError);
    }

    private void AddLog(string message)
    {
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
    }

    private void AddErrors(OperationResult result)
    {
        foreach (var error in result.Errors)
        {
            AddLog($"錯誤：{error}");
        }
    }

    private static void RefreshCommands()
    {
        System.Windows.Input.CommandManager.InvalidateRequerySuggested();
    }
}
