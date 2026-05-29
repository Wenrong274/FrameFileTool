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

    private static MainViewModel CreateSut() => new(
        Substitute.For<IFileScanner>(),
        Substitute.For<IFrameDeletePlanner>(),
        Substitute.For<IRenamePlanner>(),
        Substitute.For<IFileOperationExecutor>(),
        Substitute.For<IFolderPickerService>(),
        Substitute.For<IImageResizeExecutor>(),
        Substitute.For<IResizePreviewService>(),
        Substitute.For<IFileImportService>());

    // ── 預覽 ViewModel 建立輔助 ───────────────────────────────

    private static FrameDeletePreviewViewModel DeletePreview(bool withError = false) =>
        new(new List<OperationPreviewItem>
        {
            new() { Action = withError ? OperationAction.Error : OperationAction.Delete, HasError = withError },
        });

    private static RenamePreviewViewModel RenamePreview(bool withError = false) =>
        new(new List<OperationPreviewItem>
        {
            new() { Action = withError ? OperationAction.Error : OperationAction.Rename, HasError = withError },
        });

    private static RenamePreviewViewModel RenamePreviewWithValidItemAndError() =>
        new(new List<OperationPreviewItem>
        {
            new() { Action = OperationAction.Rename },
            new() { Action = OperationAction.Error, HasError = true },
        });

    private static RenamePreviewViewModel RenamePreviewWithExcludedSourceConflict()
    {
        var excludedItem = new OperationPreviewItem
        {
            FullPath = @"C:\imgs\b.png",
            Action = OperationAction.Rename,
            TargetName = "c.png",
        };
        excludedItem.IsIncluded = false;

        return new RenamePreviewViewModel(
        [
            new()
            {
                FullPath = @"C:\imgs\a.png",
                Action = OperationAction.Rename,
                TargetName = "b.png",
            },
            excludedItem,
        ]);
    }

    private static ResizePreviewViewModel ResizePreview(bool withError = false) =>
        new(new List<ResizePreviewItem>
        {
            new() { Action = withError ? OperationAction.Error : OperationAction.Resize, HasError = withError },
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
    // Preview CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void PreviewCommands_縮放進行中且有檔案_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.IsResizing = true;

        sut.PreviewFrameDeleteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewRenameCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void PreviewCommands_準備預覽中且有檔案_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        sut.IsPreparingPreview = true;

        sut.PreviewFrameDeleteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewRenameCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewResizeCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void PreviewSummary_準備預覽中_應顯示忙碌文字()
    {
        var sut = CreateSut();

        sut.PreviewBusyText = "正在讀取圖片尺寸…";
        sut.IsPreparingPreview = true;

        sut.PreviewSummary.Should().Be("正在讀取圖片尺寸…");
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
    public void RenamePrefix_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.RenamePrefix = "New_";

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
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
    public void IncludeSubfolders_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.IncludeSubfolders = true;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteFrameDeleteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void SelectedToolIndex_變更後_應清除既有預覽並停用執行()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.SelectedToolIndex = 1;

        sut.CurrentPreview.Should().BeNull();
        sut.ExecuteRenameCommand.CanExecute(null).Should().BeFalse();
    }
}
