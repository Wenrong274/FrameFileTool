using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;

namespace FrameFileTool.ViewModels.Tools;

/// <summary>
/// 抽幀刪除工具：持有間隔與輸出模式設定、產生預覽與執行命令。
/// 共用狀態與執行流程透過 <see cref="IToolContext"/> 協調。
/// </summary>
public sealed partial class FrameDeleteToolViewModel : ObservableObject
{
    private readonly IFrameDeletePlanner _planner;
    private readonly IFileOperationExecutor _executor;
    private readonly IToolContext _context;

    [ObservableProperty]
    private int _interval = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExecuteButtonText))]
    private FrameDeleteOutputMode _outputMode = FrameDeleteOutputMode.DeleteToRecycleBin;

    public string ExecuteButtonText => OutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
        ? "執行複製"
        : "執行刪除";

    internal FrameDeleteToolViewModel(
        IFrameDeletePlanner planner,
        IFileOperationExecutor executor,
        IToolContext context)
    {
        _planner = planner;
        _executor = executor;
        _context = context;
    }

    partial void OnIntervalChanged(int value) =>
        _context.NotifySettingChanged(PreviewTool.FrameDelete);

    partial void OnOutputModeChanged(FrameDeleteOutputMode value) =>
        _context.NotifySettingChanged(PreviewTool.FrameDelete);

    /// <summary>依目前設定產生抽幀預覽並設為當前預覽。</summary>
    internal void TriggerPreview()
    {
        var files = _context.SnapshotFiles();
        var items = OutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? _planner.Plan(files, Interval, OutputMode, targetFolderPath: string.Empty)
            : _planner.Plan(files, Interval);
        if (items is null)
            return;

        var vm = new FrameDeletePreviewViewModel(items);
        var actionText = OutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? "複製"
            : "刪除";
        var actionKind = OutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? OperationActionKind.Copy
            : OperationActionKind.Delete;
        var copyNote = OutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? "（目標資料夾待確認，衝突偵測將於執行時進行）"
            : string.Empty;
        _context.SetCurrentPreview(
            vm,
            $"抽幀預覽完成：每 {Interval} 張刪除 1 張，預計{actionText} {items.Count(i => i.ActionKind == actionKind && !i.HasError)} 個檔案。{copyNote}");
    }

    [RelayCommand(CanExecute = nameof(HasExecutablePreview))]
    private void Execute()
    {
        if (_context.CurrentPreview is not FrameDeletePreviewViewModel vm)
            return;

        OperationResult result;
        if (OutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder)
        {
            var plannedPreview = _context.PrepareCopyToTargetPreview(
                "抽幀複製",
                targetFolder => new FrameDeletePreviewViewModel(
                    _planner.Plan(
                        _context.SnapshotFiles(),
                        Interval,
                        FrameDeleteOutputMode.CopyKeptToTargetFolder,
                        targetFolder,
                        GetExistingTargetPaths(targetFolder))));
            if (plannedPreview is null)
            {
                return;
            }

            result = _executor.CopyFilesToTargetFolder(plannedPreview.Items);
        }
        else
        {
            result = _executor.DeleteToRecycleBin(vm.Items);
        }

        _context.AddLog(OutputMode == FrameDeleteOutputMode.CopyKeptToTargetFolder
            ? $"抽幀複製完成：成功 {result.SuccessCount} 個檔案。"
            : $"抽幀執行完成：已移到回收桶 {result.SuccessCount} 個檔案。");
        _context.AddErrors(result);
        _context.RescanKeepingExclusions();
    }

    private bool HasExecutablePreview() =>
        !_context.IsBatchExecuting &&
        _context.CurrentPreview is FrameDeletePreviewViewModel vm &&
        vm.HasExecutableItems &&
        !vm.HasErrors;

    /// <summary>計算保留幀複製到目標資料夾時，已存在的目標檔案路徑集合。</summary>
    private IReadOnlySet<string> GetExistingTargetPaths(string targetFolderPath)
    {
        if (string.IsNullOrWhiteSpace(targetFolderPath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var targetPaths = _context.SnapshotFiles()
            .Select(file => Path.Combine(targetFolderPath, file.Name));
        return _context.GetExistingPaths(targetPaths);
    }

    /// <summary>通知本工具的命令重新評估 CanExecute。</summary>
    internal void RefreshCommands() => ExecuteCommand.NotifyCanExecuteChanged();
}
