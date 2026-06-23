using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed class MainViewModelSourceCollapseTests
{
    [Fact]
    public void IsSourceExpanded_預設應為false()
    {
        var sut = CreateSut(Substitute.For<IFileScanner>());

        sut.IsSourceExpanded.Should().BeFalse();
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
    public void HasFolderPath_無路徑時false_設路徑後true()
    {
        var sut = CreateSut(Substitute.For<IFileScanner>());

        sut.HasFolderPath.Should().BeFalse();

        sut.SelectedFolder = @"C:\imgs";

        sut.HasFolderPath.Should().BeTrue();
    }

    [Fact]
    public void ToggleSource_應切換IsSourceExpanded()
    {
        var sut = CreateSut(Substitute.For<IFileScanner>());
        sut.IsSourceExpanded.Should().BeFalse();

        sut.ToggleSourceCommand.Execute(null);

        sut.IsSourceExpanded.Should().BeTrue();
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
