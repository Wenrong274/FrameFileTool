using FrameFileTool.Models;
using FrameFileTool.Services;
using FrameFileTool.Services.Interfaces;
using NSubstitute;

namespace FrameFileTool.Tests.Services;

public sealed class ResizePreviewServiceTests
{
    [Fact]
    public async Task BuildPreviewAsync_應先讀取尺寸再交給Planner()
    {
        var dimensionReader = Substitute.For<IImageDimensionReader>();
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        var resizePlanner = Substitute.For<IResizePlanner>();
        var file = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var options = CreateOptions();

        dimensionReader.Read(file.FullPath).Returns((100, 50));
        fileExistenceService.GetExistingPaths(Arg.Any<IEnumerable<string>>())
            .Returns(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        resizePlanner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([]);

        var sut = new ResizePreviewService(dimensionReader, fileExistenceService, resizePlanner);

        await sut.BuildPreviewAsync([file], options);

        resizePlanner.Received(1).Plan(
            Arg.Is<IReadOnlyList<FileItem>>(files =>
                files.Count == 1 &&
                files[0].Width == 100 &&
                files[0].Height == 50),
            options,
            Arg.Any<IReadOnlySet<string>>());
    }

    [Fact]
    public async Task BuildPreviewAsync_子資料夾輸出_應查詢目標檔案是否存在()
    {
        var dimensionReader = Substitute.For<IImageDimensionReader>();
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        var resizePlanner = Substitute.For<IResizePlanner>();
        var file = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var options = CreateOptions();
        var expectedPath = Path.Combine(@"C:\imgs", "resized", "a.png");
        var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { expectedPath };

        dimensionReader.Read(file.FullPath).Returns((100, 50));
        fileExistenceService.GetExistingPaths(
                Arg.Is<IEnumerable<string>>(paths => paths.SequenceEqual(new[] { expectedPath })))
            .Returns(existingPaths);
        resizePlanner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([]);

        var sut = new ResizePreviewService(dimensionReader, fileExistenceService, resizePlanner);

        await sut.BuildPreviewAsync([file], options);

        resizePlanner.Received(1).Plan(
            Arg.Any<IReadOnlyList<FileItem>>(),
            options,
            Arg.Is<IReadOnlySet<string>>(paths => paths.SetEquals(existingPaths)));
    }

    [Fact]
    public async Task BuildPreviewAsync_覆寫輸出_不應查詢目標檔案()
    {
        var dimensionReader = Substitute.For<IImageDimensionReader>();
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        var resizePlanner = Substitute.For<IResizePlanner>();
        var file = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var options = CreateOptions() with { OutputMode = ResizeOutputMode.Overwrite };

        dimensionReader.Read(file.FullPath).Returns((100, 50));
        resizePlanner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([]);

        var sut = new ResizePreviewService(dimensionReader, fileExistenceService, resizePlanner);

        await sut.BuildPreviewAsync([file], options);

        fileExistenceService.DidNotReceive().GetExistingPaths(Arg.Any<IEnumerable<string>>());
        resizePlanner.Received(1).Plan(
            Arg.Any<IReadOnlyList<FileItem>>(),
            options,
            Arg.Is<IReadOnlySet<string>>(paths => paths.Count == 0));
    }

    [Fact]
    public async Task BuildPreviewAsync_尺寸讀取失敗_應保留原始尺寸欄位()
    {
        var dimensionReader = Substitute.For<IImageDimensionReader>();
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        var resizePlanner = Substitute.For<IResizePlanner>();
        var file = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var options = CreateOptions() with { OutputMode = ResizeOutputMode.Overwrite };

        dimensionReader.Read(file.FullPath).Returns(_ => throw new InvalidOperationException());
        resizePlanner.Plan(
                Arg.Any<IReadOnlyList<FileItem>>(),
                Arg.Any<ResizeOptions>(),
                Arg.Any<IReadOnlySet<string>>())
            .Returns([]);

        var sut = new ResizePreviewService(dimensionReader, fileExistenceService, resizePlanner);

        await sut.BuildPreviewAsync([file], options);

        resizePlanner.Received(1).Plan(
            Arg.Is<IReadOnlyList<FileItem>>(files =>
                files.Count == 1 &&
                files[0].Width == 0 &&
                files[0].Height == 0),
            options,
            Arg.Any<IReadOnlySet<string>>());
    }

    private static ResizeOptions CreateOptions() =>
        new(
            Mode: ResizeMode.Percentage,
            ScalePercent: 50,
            TargetWidth: 0,
            TargetHeight: 0,
            KeepAspectRatio: true,
            OutputMode: ResizeOutputMode.Subfolder,
            SubfolderName: "resized",
            Resampler: ResamplerType.Bicubic);
}
