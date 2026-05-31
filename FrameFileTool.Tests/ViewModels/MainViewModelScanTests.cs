using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed class MainViewModelScanTests
{
    [Fact]
    public void ScanFiles_掃描結果包含錯誤_應寫入Log()
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult(
                Files:
                [
                    new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10),
                ],
                Errors:
                [
                    @"C:\imgs\private: 存取被拒",
                ]));

        var sut = CreateSut(scanner);

        sut.ScanFilesCommand.Execute(null);

        sut.Files.Should().ContainSingle();
        sut.Logs.Should().Contain(log => log.Contains("掃描錯誤") && log.Contains("存取被拒"));
    }

    [Fact]
    public void RemoveFile_剔除檔案後點選重新掃描按鈕_應重新載入被剔除的檔案()
    {
        var scanner = Substitute.For<IFileScanner>();
        var itemA = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var itemB = new FileItem(@"C:\imgs\b.png", @"C:\imgs", "b.png", ".png", 10);
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([itemA, itemB], []));

        var sut = CreateSut(scanner);
        sut.SelectedFolder = @"C:\imgs";
        sut.ScanFilesCommand.Execute(null);

        sut.Files.Count.Should().Be(2);

        sut.RemoveFileCommand.Execute(itemA);
        sut.Files.Should().ContainSingle().Which.Should().BeSameAs(itemB);

        // 使用者點選重新掃描按鈕，A 應該要回來
        sut.ScanFilesCommand.Execute(null);
        sut.Files.Count.Should().Be(2);
    }

    [Fact]
    public void RemoveFile_剔除檔案並執行改名後_自動掃描不應將剔除的檔案加回()
    {
        var scanner = Substitute.For<IFileScanner>();
        var itemA = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var itemB = new FileItem(@"C:\imgs\b.png", @"C:\imgs", "b.png", ".png", 10);
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([itemA, itemB], []));

        var executor = Substitute.For<IFileOperationExecutor>();
        executor.RenameFiles(Arg.Any<IReadOnlyList<OperationPreviewItem>>())
            .Returns(new OperationResult { SuccessCount = 1 });

        var renamePlanner = Substitute.For<IRenamePlanner>();
        renamePlanner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<string>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([new OperationPreviewItem { ActionKind = OperationActionKind.Rename }]);

        var sut = new MainViewModel(
            scanner,
            Substitute.For<IFrameDeletePlanner>(),
            renamePlanner,
            executor,
            Substitute.For<IFolderPickerService>(),
            Substitute.For<IImageResizeExecutor>(),
            Substitute.For<IResizePreviewService>(),
            Substitute.For<IFileExistenceService>(),
            Substitute.For<IFileImportService>());

        sut.SelectedFolder = @"C:\imgs";
        sut.ScanFilesCommand.Execute(null);

        // 剔除 itemA
        sut.RemoveFileCommand.Execute(itemA);
        sut.SelectedToolIndex = 1; // 改名 Tab

        // 執行改名
        sut.ExecuteRenameCommand.Execute(null);

        // 執行改名後會自動調用 RefreshScanFilesCore(keepExclusions: true)
        // 此時雖然 Scanner 會再回傳 A 和 B，但 A 被剔除了且不是點擊重新掃描按鈕，所以 A 不應回來
        sut.Files.Should().ContainSingle().Which.Should().BeSameAs(itemB);
    }

    private static MainViewModel CreateSut(IFileScanner scanner) => new(
        scanner,
        Substitute.For<IFrameDeletePlanner>(),
        Substitute.For<IRenamePlanner>(),
        Substitute.For<IFileOperationExecutor>(),
        Substitute.For<IFolderPickerService>(),
        Substitute.For<IImageResizeExecutor>(),
        Substitute.For<IResizePreviewService>(),
        Substitute.For<IFileExistenceService>(),
        Substitute.For<IFileImportService>());
}
