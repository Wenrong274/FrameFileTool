using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels.Previews;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed partial class MainViewModelCanExecuteTests
{
    // ════════════════════════════════════════════════════════
    // ExecuteResize CanExecute
    // ════════════════════════════════════════════════════════

    [Fact]
    public void ExecuteResize_預覽為null_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = null;

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽為改名型別_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = RenamePreview();

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽有錯誤_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview(withError: true);

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ExecuteResize_預覽正確且無錯誤_CanExecute應為true()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ExecuteResize_縮放項目取消勾選_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        var preview = (ResizePreviewViewModel)sut.CurrentPreview;

        preview.Items[0].IsIncluded = false;

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
        sut.PreviewSummary.Should().Be("共 0 個項目，預計縮放 0 個");
    }

    [Fact]
    public void ExecuteResize_縮放進行中且預覽正確_CanExecute應為false()
    {
        var sut = CreateSut();
        sut.CurrentPreview = ResizePreview();
        sut.ResizeTool.IsResizing = true;

        sut.ResizeTool.ExecuteCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteResize_指定資料夾模式_應先選擇資料夾再執行縮放()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"D:\out"),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize, TargetPath = @"D:\out\a.png" }]);
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"D:\out");
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        resizeExecutor.ExecuteAsync(
                Arg.Any<IEnumerable<OperationPreviewItem>>(),
                Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"D:\out"),
                Arg.Any<IProgress<ResizeProgressReport>>(),
                Arg.Any<CancellationToken>())
            .Returns(new OperationResult { SuccessCount = 1 });
        var sut = CreateSut(
            folderPicker: folderPicker,
            resizePreviewService: resizePreviewService,
            resizeExecutor: resizeExecutor,
            debounceDelay: TimeSpan.Zero);
        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));
        sut.CurrentPreview = new ResizePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Resize },
        ]);

        await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

        folderPicker.Received(1).PickFolder(Arg.Any<string>());
        await resizeExecutor.Received(1).ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"D:\out"),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteResize_指定資料夾選到來源_應使用自動解析後路徑並寫入log()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([new ResizePreviewItem { ActionKind = OperationActionKind.Resize, TargetPath = @"C:\imgs\cloud\cloud_x0.5\a.png" }]);
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        resizeExecutor
            .ExecuteAsync(
                Arg.Any<IEnumerable<OperationPreviewItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<IProgress<ResizeProgressReport>>(),
                Arg.Any<CancellationToken>())
            .Returns(new OperationResult { SuccessCount = 1 });
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"C:\imgs\cloud");
        var resolver = Substitute.For<IOutputFolderResolver>();
        resolver
            .ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", Arg.Any<ResizeOptions>())
            .Returns(new ResolvedOutputFolder(
                @"C:\imgs\cloud\cloud_x0.5",
                WasAutoRedirected: true,
                LogMessage: @"輸出資料夾與來源相同，已自動改用：C:\imgs\cloud\cloud_x0.5"));
        var sut = CreateSut(
            folderPicker: folderPicker,
            resizeExecutor: resizeExecutor,
            resizePreviewService: resizePreviewService,
            outputFolderResolver: resolver);
        sut.SelectedFolder = @"C:\imgs\cloud";
        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\cloud\a.png", @"C:\imgs\cloud", "a.png", ".png", 10));
        sut.CurrentPreview = ResizePreview();

        await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

        sut.Logs.Should().Contain(log => log.Contains(@"C:\imgs\cloud\cloud_x0.5"));
        await resizeExecutor.Received(1).ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Is<ResizeOptions>(options => options.TargetFolderPath == @"C:\imgs\cloud\cloud_x0.5"),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteResize_自動子資料夾已有衝突_應停止執行並保留錯誤預覽()
    {
        var resizePreviewService = Substitute.For<IResizePreviewService>();
        resizePreviewService
            .BuildPreviewAsync(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<CancellationToken>())
            .Returns([
                new ResizePreviewItem
                {
                    ActionKind = OperationActionKind.Error,
                    HasError = true,
                    Status = "目標檔案已存在",
                },
            ]);
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns(@"C:\imgs\cloud");
        var resolver = Substitute.For<IOutputFolderResolver>();
        resolver
            .ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", Arg.Any<ResizeOptions>())
            .Returns(new ResolvedOutputFolder(
                @"C:\imgs\cloud\cloud_x0.5",
                WasAutoRedirected: true,
                LogMessage: @"輸出資料夾與來源相同，已自動改用：C:\imgs\cloud\cloud_x0.5"));
        var sut = CreateSut(
            folderPicker: folderPicker,
            resizeExecutor: resizeExecutor,
            resizePreviewService: resizePreviewService,
            outputFolderResolver: resolver);
        sut.SelectedFolder = @"C:\imgs\cloud";
        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        sut.Files.Add(new FileItem(@"C:\imgs\cloud\a.png", @"C:\imgs\cloud", "a.png", ".png", 10));
        sut.CurrentPreview = ResizePreview();

        await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

        await resizeExecutor.DidNotReceive().ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Any<ResizeOptions>(),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
        sut.HasPreviewErrors.Should().BeTrue();
        sut.Logs.Should().Contain(log => log.Contains("縮放已停止"));
    }

    [Fact]
    public async Task ExecuteResize_指定資料夾模式取消選擇資料夾_不應執行縮放()
    {
        var folderPicker = Substitute.For<IFolderPickerService>();
        folderPicker.PickFolder(Arg.Any<string>()).Returns((string?)null);
        var resizeExecutor = Substitute.For<IImageResizeExecutor>();
        var sut = CreateSut(folderPicker: folderPicker, resizeExecutor: resizeExecutor);
        sut.ResizeTool.OutputMode = ResizeOutputMode.TargetFolder;
        sut.CurrentPreview = new ResizePreviewViewModel(
        [
            new() { ActionKind = OperationActionKind.Resize },
        ]);

        await sut.ResizeTool.ExecuteCommand.ExecuteAsync(null);

        await resizeExecutor.DidNotReceive().ExecuteAsync(
            Arg.Any<IEnumerable<OperationPreviewItem>>(),
            Arg.Any<ResizeOptions>(),
            Arg.Any<IProgress<ResizeProgressReport>>(),
            Arg.Any<CancellationToken>());
    }
}
