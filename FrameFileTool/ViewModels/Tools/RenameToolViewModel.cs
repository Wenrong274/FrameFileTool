using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;

namespace FrameFileTool.ViewModels.Tools;

/// <summary>
/// 批次改名工具：持有命名樣板、編號來源與輸出模式設定，產生預覽與執行命令。
/// 共用狀態與執行流程透過 <see cref="IToolContext"/> 協調。
/// </summary>
public sealed partial class RenameToolViewModel : ObservableObject
{
    private readonly IRenamePlanner _planner;
    private readonly IFileOperationExecutor _executor;
    private readonly IToolContext _context;

    [ObservableProperty]
    private string _template = "Symbol_[#]";

    /// <summary>是否沿用原檔名編號；為 true 時樣板的補零位數不生效。</summary>
    [ObservableProperty]
    private bool _useOriginalNumber;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExecuteButtonText))]
    private RenameOutputMode _outputMode = RenameOutputMode.RenameInPlace;

    public string ExecuteButtonText => OutputMode == RenameOutputMode.CopyToTargetFolder
        ? "執行複製改名"
        : "執行改名";

    internal RenameToolViewModel(
        IRenamePlanner planner,
        IFileOperationExecutor executor,
        IToolContext context)
    {
        _planner = planner;
        _executor = executor;
        _context = context;
    }

    partial void OnTemplateChanged(string value) =>
        _context.NotifySettingChanged(PreviewTool.Rename);

    partial void OnUseOriginalNumberChanged(bool value) =>
        _context.NotifySettingChanged(PreviewTool.Rename);

    partial void OnOutputModeChanged(RenameOutputMode value) =>
        _context.NotifySettingChanged(PreviewTool.Rename);

    /// <summary>建立目前設定對應的改名選項。</summary>
    private RenameOptions BuildOptions(string targetFolderPath) =>
        new(Template, UseOriginalNumber, OutputMode, targetFolderPath);

    /// <summary>依目前設定產生改名預覽並設為當前預覽。</summary>
    internal void TriggerPreview()
    {
        var files = _context.SnapshotFiles();
        var items = OutputMode == RenameOutputMode.CopyToTargetFolder
            ? _planner.Plan(
                files,
                BuildOptions(targetFolderPath: string.Empty),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase))
            : _planner.Plan(
                files,
                BuildOptions(targetFolderPath: string.Empty),
                files.Select(f => f.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase));
        if (items is null)
            return;

        var vm = new RenamePreviewViewModel(items);
        var actionText = OutputMode == RenameOutputMode.CopyToTargetFolder ? "複製改名" : "改名";
        var actionKind = OutputMode == RenameOutputMode.CopyToTargetFolder
            ? OperationActionKind.Copy
            : OperationActionKind.Rename;
        var actionCount = items.Count(i => i.ActionKind == actionKind && !i.HasError);
        var errorCount = items.Count(i => i.HasError);
        var renameCopyNote = OutputMode == RenameOutputMode.CopyToTargetFolder
            ? "（目標資料夾待確認，衝突偵測將於執行時進行）"
            : string.Empty;
        _context.SetCurrentPreview(
            vm,
            $"改名預覽完成：預計{actionText} {actionCount} 個檔案，錯誤 {errorCount} 個。{renameCopyNote}");
    }

    [RelayCommand(CanExecute = nameof(HasExecutablePreview))]
    private void Execute()
    {
        if (_context.CurrentPreview is not RenamePreviewViewModel vm)
            return;

        OperationResult result;
        if (OutputMode == RenameOutputMode.CopyToTargetFolder)
        {
            var plannedPreview = _context.PrepareCopyToTargetPreview(
                "複製改名",
                targetFolder => new RenamePreviewViewModel(
                    _planner.Plan(
                        _context.SnapshotFiles(),
                        BuildOptions(targetFolder) with { OutputMode = RenameOutputMode.CopyToTargetFolder },
                        GetExistingRenameTargetPaths(targetFolder))));
            if (plannedPreview is null)
            {
                return;
            }

            result = _executor.CopyRenamedFilesToTargetFolder(plannedPreview.Items);
        }
        else
        {
            result = _executor.RenameFiles(vm.Items);
        }

        _context.AddLog(OutputMode == RenameOutputMode.CopyToTargetFolder
            ? $"複製改名完成：成功 {result.SuccessCount} 個檔案。"
            : $"改名執行完成：成功 {result.SuccessCount} 個檔案。");
        _context.AddErrors(result);
        _context.RescanKeepingExclusions();
    }

    private bool HasExecutablePreview() =>
        !_context.IsBatchExecuting &&
        _context.CurrentPreview is RenamePreviewViewModel vm &&
        vm.HasExecutableItems &&
        !vm.HasErrors;

    /// <summary>以與規劃相同的命名公式，計算複製改名時已存在的目標檔案路徑集合。</summary>
    private IReadOnlySet<string> GetExistingRenameTargetPaths(string targetFolderPath)
    {
        if (string.IsNullOrWhiteSpace(targetFolderPath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var targetPaths = _planner.ProjectTargetPaths(
            _context.SnapshotFiles(),
            BuildOptions(targetFolderPath) with { OutputMode = RenameOutputMode.CopyToTargetFolder });

        return _context.GetExistingPaths(targetPaths);
    }

    /// <summary>通知本工具的命令重新評估 CanExecute。</summary>
    internal void RefreshCommands() => ExecuteCommand.NotifyCanExecuteChanged();
}
