using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using FrameFileTool.ViewModels.Previews;
using FrameFileTool.ViewModels.Tools;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed class MainViewModelDropImportTests
{
    [Fact]
    public void ImportDroppedPaths_匯入檔案_應加入清單更新摘要並自動觸發預覽()
    {
        var importService = Substitute.For<IFileImportService>();
        var frameDeletePlanner = Substitute.For<IFrameDeletePlanner>();
        var file = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);

        importService.Import(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns(new FileImportResult([file], []));

        frameDeletePlanner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<int>())
            .Returns(Array.Empty<OperationPreviewItem>());

        var sut = CreateSut(importService, frameDeletePlanner);

        sut.ImportDroppedPathsCommand.Execute([file.FullPath]);

        sut.Files.Should().ContainSingle();
        sut.FileSummary.Should().Be("已載入 1 個圖片檔");
        sut.CurrentPreview.Should().BeOfType<FrameDeletePreviewViewModel>();
        sut.Logs.Should().Contain(log => log.Contains("拖放匯入完成") && log.Contains("新增 1"));
    }

    [Fact]
    public void ImportDroppedPaths_應傳入目前副檔名與子資料夾設定()
    {
        var importService = Substitute.For<IFileImportService>();
        importService.Import(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns(new FileImportResult([], []));

        var sut = CreateSut(importService);
        sut.IncludePng = true;
        sut.IncludeJpg = false;
        sut.IncludeJpeg = false;
        sut.IncludeWebp = true;
        sut.IncludeBmp = false;
        sut.IncludeSubfolders = true;

        sut.ImportDroppedPathsCommand.Execute([@"C:\imgs"]);

        importService.Received(1).Import(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Is<IEnumerable<string>>(extensions =>
                extensions.SequenceEqual(new[] { ".png", ".webp" })),
            true,
            Arg.Any<IReadOnlySet<string>>());
    }

    [Fact]
    public void ImportDroppedPaths_既有檔案路徑_應傳給匯入服務避免重複()
    {
        var importService = Substitute.For<IFileImportService>();
        var existing = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        importService.Import(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns(new FileImportResult([], []));

        var sut = CreateSut(importService);
        sut.Files.Add(existing);

        sut.ImportDroppedPathsCommand.Execute([@"C:\imgs\a.png"]);

        importService.Received(1).Import(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<bool>(),
            Arg.Is<IReadOnlySet<string>>(paths => paths.Contains(existing.FullPath)));
    }

    [Fact]
    public void ImportDroppedPaths_只有錯誤_應寫入Log且不清除預覽()
    {
        var importService = Substitute.For<IFileImportService>();
        importService.Import(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns(new FileImportResult([], ["a.txt: 副檔名不支援"]));

        var preview = new RenamePreviewViewModel(
        [
            new OperationPreviewItem { ActionKind = OperationActionKind.Rename },
        ]);
        var sut = CreateSut(importService);
        sut.CurrentPreview = preview;

        sut.ImportDroppedPathsCommand.Execute([@"C:\imgs\a.txt"]);

        sut.Files.Should().BeEmpty();
        sut.CurrentPreview.Should().BeSameAs(preview);
        sut.Logs.Should().Contain(log => log.Contains("拖放略過") && log.Contains("副檔名不支援"));
    }

    [Fact]
    public void ImportDroppedPaths_縮放進行中_CanExecute應為false()
    {
        var sut = CreateSut(Substitute.For<IFileImportService>());

        sut.ResizeTool.IsResizing = true;

        sut.ImportDroppedPathsCommand.CanExecute([@"C:\imgs\a.png"]).Should().BeFalse();
    }

    [Fact]
    public void ImportDroppedPaths_空路徑_不應呼叫匯入服務()
    {
        var importService = Substitute.For<IFileImportService>();
        var sut = CreateSut(importService);

        sut.ImportDroppedPathsCommand.Execute(Array.Empty<string>());

        importService.DidNotReceive().Import(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<bool>(),
            Arg.Any<IReadOnlySet<string>>());
    }

    [Fact]
    public void RescanKeepingExclusions_沒有來源資料夾_不應清空拖放匯入的檔案()
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([], []));

        var sut = CreateSut(Substitute.For<IFileImportService>(), scanner: scanner);
        sut.Files.Add(new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10));

        ((IToolContext)sut).RescanKeepingExclusions();

        sut.Files.Should().ContainSingle();
        scanner.DidNotReceive().Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>());
    }

    private static MainViewModel CreateSut(
        IFileImportService importService,
        IFrameDeletePlanner? frameDeletePlanner = null,
        IFileScanner? scanner = null) => new(
        scanner ?? Substitute.For<IFileScanner>(),
        frameDeletePlanner ?? Substitute.For<IFrameDeletePlanner>(),
        Substitute.For<IRenamePlanner>(),
        Substitute.For<IFileOperationExecutor>(),
        Substitute.For<IFolderPickerService>(),
        Substitute.For<IImageResizeExecutor>(),
        Substitute.For<IResizePreviewService>(),
        Substitute.For<IOutputFolderResolver>(),
        Substitute.For<IDenoisePlanner>(),
        Substitute.For<IDenoiseExecutor>(),
        Substitute.For<IDenoisePreviewService>(),
        Substitute.For<IFileExistenceService>(),
        importService,
        Substitute.For<IUpdateService>(),
        Substitute.For<IExternalLinkService>());
}
