using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed partial class MainViewModelCanExecuteTests
{
    // ════════════════════════════════════════════════════════
    // ExecuteFrameDelete CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteFrameDelete_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽為改名型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview(withError: true);

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteFrameDelete_刪除項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        var preview = (FrameDeletePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計刪除 0 個");
    }

    [Fact]
    public void ExecuteFrameDelete_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = DeletePreview();
        sut.ResizeTool.IsResizing = true;

        sut.FrameDeleteTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteFrameDelete_複製模式_應先選擇資料夾再呼叫複製執行器()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<int>(),
                FrameDeleteOutputMode.CopyKeptToTargetFolder,
                @"D:\out",
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Copy, TargetPath = @"D:\out\a.png" }]);
        var executor = Substitute.For<IFileOperationExecutor>();
        executor.CopyFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>())
            .Returns(new OperationResult { SuccessCount = 1 });
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        fileExistenceService.GetExistingPaths(Arg.Any<IEnumerable<string>>())
            .Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var sut = CreateSut(
            frameDeletePlanner: planner,
            executor: executor,
            folderPicker: folderPicker,
            fileExistenceService: fileExistenceService);
        sut.FrameDeleteTool.OutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new FrameDeletePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.FrameDeleteTool.ExecuteCommand.Execute(null);

        folderPicker.Received(1).PickFolder(Arg.Any<string>());
        planner.Received(1).Plan(
            Arg.Any<IReadOnlyList<FileItem>>(),
            Arg.Any<int>(),
            FrameDeleteOutputMode.CopyKeptToTargetFolder,
            @"D:\out",
            Arg.Any<IReadOnlySet<string>>());
        executor.Received(1).CopyFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
        executor.DidNotReceive().DeleteToRecycleBin(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    [Fact]
    public void ExecuteFrameDelete_複製模式取消選擇資料夾_不應執行複製()
    {
        var executor = Substitute.For<IFileOperationExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns((string?)null);
        var sut = CreateSut(executor: executor, folderPicker: folderPicker);
        sut.FrameDeleteTool.OutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;
        sut.CurrentPreview = new FrameDeletePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.FrameDeleteTool.ExecuteCommand.Execute(null);

        executor.DidNotReceive().CopyFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
    }

    [Fact]
    public void ExecuteFrameDelete_複製模式目標資料夾有衝突_應停止執行並記錄log()
    {
        var planner = Substitute.For<IFrameDeletePlanner>();
        planner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<int>(),
                FrameDeleteOutputMode.CopyKeptToTargetFolder,
                @"D:\out",
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Error, HasError = true }]);
        var executor = Substitute.For<IFileOperationExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var sut = CreateSut(frameDeletePlanner: planner, executor: executor, folderPicker: folderPicker);
        sut.FrameDeleteTool.OutputMode = FrameDeleteOutputMode.CopyKeptToTargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new FrameDeletePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Copy },
        ]);

        sut.FrameDeleteTool.ExecuteCommand.Execute(null);

        executor.DidNotReceive().CopyFilesToTargetFolder(Arg.Any<IEnumerable<OperationPreviewItem>>());
        sut.Logs.Should().Contain(log => log.Contains("抽幀複製已停止"));
        sut.HasPreviewErrors.Should().BeTrue();
    }
}
