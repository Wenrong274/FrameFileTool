using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

/// <summary>
/// 測試 MainViewModel 四個執行指令（抽幀刪除、批次改名、批次縮放、批次降噪）的
/// CanExecute 邏輯，驗證 CurrentPreview 型別判斷與批次執行中的保護機制。
/// </summary>
public sealed partial class MainViewModelCanExecuteTests
{
    // ── 共用工廠 ──────────────────────────────────────────────

    private static MainViewModel CreateSut(
        IFrameDeletePlanner? frameDeletePlanner = null,
        IRenamePlanner? renamePlanner = null,
        IFileOperationExecutor? executor = null,
        IFolderPickerService? folderPicker = null,
        IImageResizeExecutor? resizeExecutor = null,
        IFileExistenceService? fileExistenceService = null,
        IResizePreviewService? resizePreviewService = null,
        IOutputFolderResolver? outputFolderResolver = null,
        IDenoisePlanner? denoisePlanner = null,
        IDenoiseExecutor? denoiseExecutor = null,
        IDenoisePreviewService? denoisePreviewService = null,
        TimeSpan debounceDelay = default)
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([], []));
        var effectiveOutputFolderResolver = outputFolderResolver ?? Substitute.For<IOutputFolderResolver>();
        if (outputFolderResolver is null)
        {
            effectiveOutputFolderResolver
                .ResolveForResize(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<ResizeOptions>())
                .Returns(call => new ResolvedOutputFolder(call.ArgAt<string>(1), WasAutoRedirected: false, LogMessage: null));
        }

        return new MainViewModel(
        scanner,
        frameDeletePlanner ?? Substitute.For<IFrameDeletePlanner>(),
        renamePlanner ?? Substitute.For<IRenamePlanner>(),
        executor ?? Substitute.For<IFileOperationExecutor>(),
        folderPicker ?? Substitute.For<IFolderPickerService>(),
        resizeExecutor ?? Substitute.For<IImageResizeExecutor>(),
        resizePreviewService ?? Substitute.For<IResizePreviewService>(),
        effectiveOutputFolderResolver,
        denoisePlanner ?? Substitute.For<IDenoisePlanner>(),
        denoiseExecutor ?? Substitute.For<IDenoiseExecutor>(),
        denoisePreviewService ?? Substitute.For<IDenoisePreviewService>(),
        fileExistenceService ?? Substitute.For<IFileExistenceService>(),
        Substitute.For<IFileImportService>(),
        Substitute.For<IUpdateService>(),
        Substitute.For<IExternalLinkService>(),
        debounceDelay);
    }

    // ── 預覽 ViewModel 建立輔助 ───────────────────────────────

    private static FrameDeletePreviewViewModel DeletePreview(bool withError = false) =>
        new(new List<OperationPreviewItem>
        {
            new() { ActionKind = withError ? OperationActionKind.Error : OperationActionKind.Delete, HasError = withError },
        });

    private static RenamePreviewViewModel RenamePreview(bool withError = false) =>
        new(new List<OperationPreviewItem>
        {
            new() { ActionKind = withError ? OperationActionKind.Error : OperationActionKind.Rename, HasError = withError },
        });

    private static RenamePreviewViewModel RenamePreviewWithValidItemAndError() =>
        new(new List<OperationPreviewItem>
        {
            new() { ActionKind = OperationActionKind.Rename },
            new() { ActionKind = OperationActionKind.Error, HasError = true },
        });

    private static RenamePreviewViewModel RenamePreviewWithExcludedSourceConflict()
    {
        var excludedItem = new OperationPreviewItem
        {
            FullPath = @"C:\imgs\b.png",
            ActionKind = OperationActionKind.Rename,
            TargetName = "c.png",
        };
        excludedItem.IsIncluded = false;

        return new RenamePreviewViewModel(
        [
            new()
            {
                FullPath = @"C:\imgs\a.png",
                ActionKind = OperationActionKind.Rename,
                TargetName = "b.png",
            },
            excludedItem,
        ]);
    }

    private static ResizePreviewViewModel ResizePreview(bool withError = false) =>
        new(new List<ResizePreviewItem>
        {
            new() { ActionKind = withError ? OperationActionKind.Error : OperationActionKind.Resize, HasError = withError },
        });

    private static DenoisePreviewViewModel DenoisePreview(bool withError = false) =>
        new(new List<OperationPreviewItem>
        {
            new() { ActionKind = withError ? OperationActionKind.Error : OperationActionKind.Denoise, HasError = withError },
        });

    // ════════════════════════════════════════════════════════
    // Preview invalidation
    // ════════════════════════════════════════════════════════

    [Fact]
    public void FrameDeleteInterval_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.FrameDeleteTool.Interval = 3;

        sut.CurrentPreview.Should().BeNull();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void FrameDeleteInterval_變更時_不應清除改名預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.FrameDeleteTool.Interval = 3;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Rename命名樣板_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.RenameTool.Template = "New_[#]";

        sut.CurrentPreview.Should().BeNull();
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Rename命名樣板_變更時_不應清除抽幀預覽()
    {
        var sut = CreateSut();
        var preview = DeletePreview();
        sut.CurrentPreview = preview;

        sut.RenameTool.Template = "New_[#]";

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ScaleFactor_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.ResizeTool.ScaleFactor = 0.75;

        sut.CurrentPreview.Should().BeNull();
        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ScaleFactor_變更時_不應清除改名預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.ResizeTool.ScaleFactor = 0.75;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void IncludeSubfolders_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.IncludeSubfolders = true;

        sut.CurrentPreview.Should().BeNull();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedTool_變更到不同工具_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.SelectedTool = PreviewTool.Rename;

        sut.CurrentPreview.Should().BeNull();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedTool_變更到預覽所屬工具_不應清除既有預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.SelectedTool = PreviewTool.Rename;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ClearFolderAndFiles_執行後_應清空路徑與檔案且重設預覽()
    {
        var sut = CreateSut();
        sut.SelectedFolder = @"C:\imgs";
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = DeletePreview();

        sut.ClearFolderAndFilesCommand.Execute(null);

        sut.SelectedFolder.Should().BeEmpty();
        sut.Files.Should().BeEmpty();
        sut.CurrentPreview.Should().BeNull();
        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RemoveFile_移除指定項目_應從檔案清單移出並重新計算預覽()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<int>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Delete }]);

        var sut = CreateSut(frameDeletePlanner: planner);
        var itemA = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var itemB = new FileItem(@"C:\imgs\b.png", @"C:\imgs", "b.png", ".png", 10);
        sut.Files.Add(itemA);
        sut.Files.Add(itemB);

        sut.RemoveFileCommand.Execute(itemA);

        sut.Files.Should().ContainSingle().Which.Should().BeSameAs(itemB);
        planner.Received(1).Plan(
            Arg.Is<IReadOnlyList<FileItem>>(list => list.Count == 1 && list[0] == itemB),
            Arg.Any<int>());
    }
}
