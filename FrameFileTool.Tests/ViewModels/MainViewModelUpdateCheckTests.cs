using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using NSubstitute;

namespace FrameFileTool.Tests.ViewModels;

public sealed class MainViewModelUpdateCheckTests
{
    [Fact]
    public async Task 啟動更新檢查後_偵測到新版_應顯示更新橫幅()
    {
        var updateService = Substitute.For<IUpdateService>();
        updateService.CheckForUpdateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new UpdateInfo(true, "v1.3.0", "https://github.com/example/releases/tag/v1.3.0")));
        var sut = CreateSut(updateService);

        sut.StartUpdateCheck();
        await sut.UpdateCheckTask;

        sut.IsUpdateAvailable.Should().BeTrue();
        sut.LatestVersionText.Should().Be("v1.3.0");
        sut.LatestReleaseUrl.Should().Be("https://github.com/example/releases/tag/v1.3.0");
        sut.GoToDownloadPageCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task 啟動更新檢查後_沒有新版_應保持橫幅隱藏()
    {
        var updateService = Substitute.For<IUpdateService>();
        updateService.CheckForUpdateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UpdateInfo.None));
        var sut = CreateSut(updateService);

        sut.StartUpdateCheck();
        await sut.UpdateCheckTask;

        sut.IsUpdateAvailable.Should().BeFalse();
        sut.GoToDownloadPageCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task GoToDownloadPage_有更新網址_應交由外部連結服務開啟()
    {
        var updateService = Substitute.For<IUpdateService>();
        updateService.CheckForUpdateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new UpdateInfo(true, "v1.3.0", "https://github.com/example/releases/tag/v1.3.0")));
        var externalLinkService = Substitute.For<IExternalLinkService>();
        var sut = CreateSut(updateService, externalLinkService);

        sut.StartUpdateCheck();
        await sut.UpdateCheckTask;
        sut.GoToDownloadPageCommand.Execute(null);

        externalLinkService.Received(1).Open("https://github.com/example/releases/tag/v1.3.0");
    }

    [Fact]
    public async Task DismissUpdateBanner_關閉後同次執行不應再顯示()
    {
        var releaseReady = new TaskCompletionSource<UpdateInfo>();
        var updateService = Substitute.For<IUpdateService>();
        updateService.CheckForUpdateAsync(Arg.Any<CancellationToken>())
            .Returns(releaseReady.Task);
        var sut = CreateSut(updateService);
        sut.StartUpdateCheck();

        sut.DismissUpdateBannerCommand.Execute(null);
        releaseReady.SetResult(new UpdateInfo(true, "v1.3.0", "https://github.com/example/releases/tag/v1.3.0"));
        await sut.UpdateCheckTask;

        sut.IsUpdateAvailable.Should().BeFalse();
        sut.GoToDownloadPageCommand.CanExecute(null).Should().BeFalse();
    }

    private static MainViewModel CreateSut(
        IUpdateService updateService,
        IExternalLinkService? externalLinkService = null)
    {
        var scanner = Substitute.For<IFileScanner>();
        scanner.Scan(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<bool>())
            .Returns(new FileScanResult([], []));

        return new MainViewModel(
            scanner,
            Substitute.For<IFrameDeletePlanner>(),
            Substitute.For<IRenamePlanner>(),
            Substitute.For<IFileOperationExecutor>(),
            Substitute.For<IFolderPickerService>(),
            Substitute.For<IImageResizeExecutor>(),
            Substitute.For<IResizePreviewService>(),
            Substitute.For<IDenoisePlanner>(),
            Substitute.For<IDenoiseExecutor>(),
            Substitute.For<IDenoisePreviewService>(),
            Substitute.For<IFileExistenceService>(),
            Substitute.For<IFileImportService>(),
            updateService,
            externalLinkService ?? Substitute.For<IExternalLinkService>());
    }
}
