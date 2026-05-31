using FluentAssertions;
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
    public async Task BuildPreviewAsync_尺寸讀取失敗_應保留錯誤資訊給Planner()
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
                files[0].Height == 0 &&
                files[0].DimensionReadError == "無法讀取圖片尺寸，請確認檔案內容是有效圖片"),
            options,
            Arg.Any<IReadOnlySet<string>>());
    }

    [Fact]
    public async Task BuildPreviewAsync_尺寸讀取失敗_應產生錯誤預覽項目()
    {
        var dimensionReader = Substitute.For<IImageDimensionReader>();
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        var file = new FileItem(@"C:\imgs\bad.png", @"C:\imgs", "bad.png", ".png", 10);
        var options = CreateOptions() with { OutputMode = ResizeOutputMode.Overwrite };

        dimensionReader.Read(file.FullPath).Returns(_ => throw new InvalidOperationException("不是有效圖片"));
        var sut = new ResizePreviewService(dimensionReader, fileExistenceService, new ResizePlanner());

        var result = await sut.BuildPreviewAsync([file], options);

        result.Should().ContainSingle();
        result[0].HasError.Should().BeTrue();
        result[0].ActionKind.Should().Be(OperationActionKind.Error);
        result[0].Status.Should().Contain("無法讀取圖片尺寸");
    }

    [Fact]
    public async Task BuildPreviewAsync_倍率模式_預覽文字應使用倍率語意()
    {
        var dimensionReader = Substitute.For<IImageDimensionReader>();
        var fileExistenceService = Substitute.For<IFileExistenceService>();
        var file = new FileItem(@"C:\imgs\a.png", @"C:\imgs", "a.png", ".png", 10);
        var options = CreateOptions() with { OutputMode = ResizeOutputMode.Overwrite };

        dimensionReader.Read(file.FullPath).Returns((100, 50));
        var sut = new ResizePreviewService(dimensionReader, fileExistenceService, new ResizePlanner());

        var result = await sut.BuildPreviewAsync([file], options);

        result.Should().ContainSingle();
        result[0].Status.Should().Contain("0.5 倍");
        result[0].Status.Should().NotContain("%");
        result[0].Status.Should().NotContain("百分比");
    }

    private static ResizeOptions CreateOptions() =>
        new(
            Mode: ResizeMode.ScaleFactor,
            ScaleFactor: 0.5,
            TargetWidth: 0,
            TargetHeight: 0,
            KeepAspectRatio: true,
            OutputMode: ResizeOutputMode.Subfolder,
            SubfolderName: "resized",
            Resampler: ResamplerType.Bicubic);
}
