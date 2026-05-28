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

    private static MainViewModel CreateSut(IFileScanner scanner) => new(
        scanner,
        Substitute.For<IFrameDeletePlanner>(),
        Substitute.For<IRenamePlanner>(),
        Substitute.For<IFileOperationExecutor>(),
        Substitute.For<IFolderPickerService>(),
        Substitute.For<IImageResizeExecutor>(),
        Substitute.For<IResizePreviewService>(),
        Substitute.For<IFileImportService>());
}
