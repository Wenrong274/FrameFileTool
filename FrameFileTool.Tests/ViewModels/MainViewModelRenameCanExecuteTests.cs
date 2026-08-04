using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed partial class MainViewModelCanExecuteTests
{
    // ════════════════════════════════════════════════════════
    // ExecuteRename CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteRename_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽為抽幀刪除型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview(withError: true);

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_預覽同時有可改名項目與錯誤列_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreviewWithValidItemAndError();

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_已勾選改名目標撞到未勾選來源檔_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreviewWithExcludedSourceConflict();

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.HasPreviewErrors.Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteRename_改名項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();
        var preview = (RenamePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計改名 0 個");
    }

    [Fact]
    public void ExecuteRename_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();
        sut.ResizeTool.IsResizing = true;

        sut.RenameTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteRename_複製模式_應先選擇資料夾再呼叫複製改名執行器()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Is<RenameOptions>(o => o.OutputMode == RenameOutputMode.CopyToTargetFolder && o.TargetFolderPath == @"D:\out"),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Copy, TargetPath = @"D:\out\F_0.png" }]);
        var executor = Substitute.For<IFileOperationExecutor>();
        executor.CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>())
            .Returns(new OperationResult { SuccessCount = 1 });
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        fileExistenceService.GetExistingPaths(Arg.Any<IEnumerable<string>>())
            .Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var sut = CreateSut(
            renamePlanner: planner,
            executor: executor,
            folderPicker: folderPicker,
            fileExistenceService: fileExistenceService);
        sut.RenameTool.OutputMode = RenameOutputMode.CopyToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new RenamePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.RenameTool.ExecuteCommand.Execute(null);

        folderPicker.Received(1).PickFolder(Arg.Any<string>());
        planner.Received(1).Plan(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Is<RenameOptions>(o => o.OutputMode == RenameOutputMode.CopyToTargetFolder && o.TargetFolderPath == @"D:\out"),
            Arg.Any<IReadOnlySet<string>>());
        executor.Received(1).CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
        executor.DidNotReceive().RenameFiles(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    [Fact]
    public void ExecuteRename_複製模式取消選擇資料夾_不應執行複製改名()
    {
        var executor = Substitute.For<IFileOperationExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns((string?)null);
        var sut = CreateSut(executor: executor, folderPicker: folderPicker);
        sut.RenameTool.OutputMode = RenameOutputMode.CopyToTargetFolder;
        sut.CurrentPreview = new RenamePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.RenameTool.ExecuteCommand.Execute(null);

        executor.DidNotReceive().CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    [Fact]
    public void ExecuteRename_複製模式目標資料夾有衝突_應停止執行並記錄log()
    {
        var planner = Substitute.For<IRenamePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Is<RenameOptions>(o => o.OutputMode == RenameOutputMode.CopyToTargetFolder && o.TargetFolderPath == @"D:\out"),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Error, HasError = true }]);
        var executor = Substitute.For<IFileOperationExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var sut = CreateSut(renamePlanner: planner, executor: executor, folderPicker: folderPicker);
        sut.RenameTool.OutputMode = RenameOutputMode.CopyToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new RenamePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.RenameTool.ExecuteCommand.Execute(null);

        executor.DidNotReceive().CopyRenamedFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
        sut.Logs.Should().Contain(log => log.Contains("複製改名已停止"));
        sut.HasPreviewErrors.Should().BeTrue();
    }
}
