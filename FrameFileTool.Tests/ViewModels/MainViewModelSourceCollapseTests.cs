using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed class MainViewModelSourceCollapseTests
{
    [Fact]
    public void IsSourceExpanded_預設應為true()
    {
        var sut = CreateSut(Substitute.For<IFileScanner>());

        sut.IsSourceExpanded.Should().BeTrue();
    }

    [Fact]
    public void HasSource_無檔案時false_掃描出檔案後true()
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult(
                [new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10)], []));
        var sut = CreateSut(scanner);

        sut.HasSource.Should().BeFalse();

        sut.SelectedFolder = @"C:\imgs";
        sut.ScanFilesCommand.Execute(null);

        sut.HasSource.Should().BeTrue();
    }

    [Fact]
    public void ScanFiles_成功掃描出檔案後_IsSourceExpanded應自動收合為false()
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult(
                [new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10)], []));
        var sut = CreateSut(scanner);
        sut.SelectedFolder = @"C:\imgs";

        sut.ScanFilesCommand.Execute(null);

        sut.IsSourceExpanded.Should().BeFalse();
    }

    [Fact]
    public void ScanFiles_掃描結果為空_IsSourceExpanded應維持展開()
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([], []));
        var sut = CreateSut(scanner);
        sut.SelectedFolder = @"C:\imgs";

        sut.ScanFilesCommand.Execute(null);

        sut.IsSourceExpanded.Should().BeTrue();
    }

    [Fact]
    public void ToggleSource_應切換IsSourceExpanded()
    {
        var sut = CreateSut(Substitute.For<IFileScanner>());
        sut.IsSourceExpanded = false;

        sut.ToggleSourceCommand.Execute(null);

        sut.IsSourceExpanded.Should().BeTrue();
    }

    [Fact]
    public async Task ImportDroppedPaths_拖放匯入出檔案後_IsSourceExpanded應自動收合為false()
    {
        var importService = Substitute.For<IFileImportService>();
        importService.Import(
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns(new FileImportResult(
                [new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10)], []));
        var sut = CreateSut(Substitute.For<IFileScanner>(), importService);

        await sut.ImportDroppedPathsCommand.ExecuteAsync([@"C:\imgs"]);

        sut.IsSourceExpanded.Should().BeFalse();
    }

    private static MainViewModel CreateSut(
        IFileScanner scanner,
        IFileImportService? importService = null) => new(
        scanner,
        Substitute.For<IFrameDeletePlanner>(),
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
        importService ?? Substitute.For<IFileImportService>(),
        Substitute.For<IUpdateService>(),
        Substitute.For<IExternalLinkService>());
}
