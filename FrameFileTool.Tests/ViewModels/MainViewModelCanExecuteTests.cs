using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

/// <summary>
/// 測試 MainViewModel 三個執行指令（抽幀刪除、批次改名、批次縮放）的
/// CanExecute 邏輯，驗證 CurrentPreview 型別判斷與 IsResizing 保護機制。
/// </summary>
public sealed class MainViewModelCanExecuteTests
{
    // ── 共用工廠 ──────────────────────────────────────────────

    private static MainViewModel CreateSut(
        IFrameDeletePlanner? frameDeletePlanner = null,
        IRenamePlanner? renamePlanner = null,
        IResizePreviewService? resizePreviewService = null,
        TimeSpan debounceDelay = default) => new(
        Substitute.For<IFileScanner>(),
        frameDeletePlanner ?? Substitute.For<IFrameDeletePlanner>(),
        renamePlanner ?? Substitute.For<IRenamePlanner>(),
        Substitute.For<IFileOperationExecutor>(),
        Substitute.For<IFolderPickerService>(),
        Substitute.For<IImageResizeExecutor>(),
        resizePreviewService ?? Substitute.For<IResizePreviewService>(),
        Substitute.For<IFileImportService>(),
        debounceDelay);

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

    // ════════════════════════════════════════════════════════
    // ExecuteFrameDelete CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteFrameDelete_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽為改名型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview(withError: true);

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteFrameDelete_刪除項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        var preview = (FrameDeletePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計刪除 0 個");
    }

    [Fact]
    public void ExecuteFrameDelete_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        sut.IsResizing = true;

        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════
    // ExecuteRename CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteRename_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽為抽幀刪除型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview(withError: true);

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽同時有可改名項目與錯誤列_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreviewWithValidItemAndError();

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_已勾選改名目標撞到未勾選來源檔_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreviewWithExcludedSourceConflict();

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_改名項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();
        var preview = (RenamePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計改名 0 個");
    }

    [Fact]
    public void ExecuteRename_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();
        sut.IsResizing = true;

        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════
    // ExecuteResize CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteResize_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽為改名型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview(withError: true);

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteResize_縮放項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        var preview = (ResizePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計縮放 0 個");
    }

    [Fact]
    public void ExecuteResize_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        sut.IsResizing = true;

        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════
    // 即時預覽觸發（Live Preview）
    // ════════════════════════════════════════════════════════

    [Fact]
    public void PreviewSummary_準備預覽中_應顯示忙碌文字()
    {
        var sut = CreateSut();

        sut.PreviewBusyText = "正在讀取圖片尺寸…";
        sut.IsPreparingPreview = true;

        sut.PreviewSummary.Should().Be("正在讀取圖片尺寸…");
    }

    [Fact]
    public void FrameDeleteInterval_變更且有檔案_應即時更新預覽()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<int>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Delete }]);
        var sut = CreateSut(frameDeletePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.FrameDeleteInterval = 3;

        sut.CurrentPreview.Should().BeOfType<FrameDeletePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("抽幀預覽完成"));
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RenamePrefix_變更且有檔案_應即時更新預覽()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Rename }]);
        var sut = CreateSut(renamePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedToolIndex = 1;

        sut.RenamePrefix = "New_";

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("改名預覽完成"));
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task ScalePercent_變更且有檔案_應觸發防抖縮放預覽()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize }]);
        var sut = CreateSut(resizePreviewService: resizePreviewService, debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedToolIndex = 2;

        sut.ScalePercent = 75;
        await sut.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("縮放預覽完成"));
        sut.ExecuteResizeCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void SelectedToolIndex_切換到抽幀刪除且有檔案_應即時產生預覽()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(Arg.Any<IReadOnlyList<FileItem>>(), Arg.Any<int>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Delete }]);
        var sut = CreateSut(frameDeletePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedToolIndex = 1;

        sut.SelectedToolIndex = 0;

        sut.CurrentPreview.Should().BeOfType<FrameDeletePreviewViewModel>();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void SelectedToolIndex_切換到批次改名且有檔案_應即時產生預覽()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Rename }]);
        var sut = CreateSut(renamePlanner: planner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.SelectedToolIndex = 1;

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task SelectedToolIndex_切換到批次縮放且有檔案_應觸發防抖縮放預覽()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize }]);
        var sut = CreateSut(resizePreviewService: resizePreviewService, debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.SelectedToolIndex = 2;
        await sut.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        sut.ExecuteResizeCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task 縮放設定連續變更_最終預覽反映最新設定()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize }]);

        var sut = CreateSut(resizePreviewService: resizePreviewService, debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.SelectedToolIndex = 2;

        // 快速連續變更：每次都取消前一個 debounce，只有最後一個會執行
        sut.ScalePercent = 60;
        sut.ScalePercent = 75;

        await sut.LivePreviewTask;

        sut.CurrentPreview.Should().BeOfType<ResizePreviewViewModel>();
        sut.IsPreparingPreview.Should().BeFalse();
    }

    [Fact]
    public async Task 縮放預覽進行中切換工具_最終顯示新工具預覽()
    {
        var resizePending = new TaskCompletionSource<IReadOnlyList<ResizePreviewItem>>();
        var resizeCallCount = 0;

        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                resizeCallCount++;
                return resizePending.Task;
            });

        var renamePlanner = Substitute.For<IRenamePlanner>();
        renamePlanner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Rename }]);

        var sut = CreateSut(
            renamePlanner: renamePlanner,
            resizePreviewService: resizePreviewService,
            debounceDelay: TimeSpan.Zero);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.SelectedToolIndex = 2;

        // 切換到改名 Tab → 取消縮放 debounce（debounce 尚未進入 BuildPreviewAsync），
        // 同步產生改名預覽
        var taskBeforeSwitch = sut.LivePreviewTask;
        sut.SelectedToolIndex = 1;

        // 讓縮放 debounce 排程執行（確認即使它啟動也被取消）
        await taskBeforeSwitch;

        sut.CurrentPreview.Should().BeOfType<RenamePreviewViewModel>();
        sut.IsPreparingPreview.Should().BeFalse();
    }

    // ════════════════════════════════════════════════════════
    // Preview invalidation
    // ════════════════════════════════════════════════════════

    [Fact]
    public void FrameDeleteInterval_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.FrameDeleteInterval = 3;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void FrameDeleteInterval_變更時_不應清除改名預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.FrameDeleteInterval = 3;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RenamePrefix_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.RenamePrefix = "New_";

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RenamePrefix_變更時_不應清除抽幀預覽()
    {
        var sut = CreateSut();
        var preview = DeletePreview();
        sut.CurrentPreview = preview;

        sut.RenamePrefix = "New_";

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ScalePercent_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.ScalePercent = 75;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ScalePercent_變更時_不應清除改名預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.ScalePercent = 75;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void IncludeSubfolders_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.IncludeSubfolders = true;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedToolIndex_變更到不同工具_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.SelectedToolIndex = 1;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedToolIndex_變更到預覽所屬工具_不應清除既有預覽()
    {
        var sut = CreateSut();
        var preview = RenamePreview();
        sut.CurrentPreview = preview;

        sut.SelectedToolIndex = 1;

        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeTrue();
    }
}
