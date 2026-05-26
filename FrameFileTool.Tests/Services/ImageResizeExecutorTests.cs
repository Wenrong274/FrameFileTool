using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class ImageResizeExecutorTests
{
    private readonly ImageResizeExecutor _sut = new();

    [Fact]
    public async Task ExecuteAsync_取消權杖已取消_應回傳取消結果並略過項目()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var items = new[]
        {
            new OperationPreviewItem
            {
                FullPath = @"C:\imgs\a.png",
                OriginalName = "a.png",
                Action = OperationAction.Resize,
            },
        };

        var result = await _sut.ExecuteAsync(items, Options(), cancellationToken: cts.Token);

        result.Canceled.Should().BeTrue();
        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Execute_子資料夾名稱包含路徑_應拒絕執行()
    {
        var items = new[]
        {
            new OperationPreviewItem
            {
                FullPath = @"C:\imgs\a.png",
                OriginalName = "a.png",
                Action = OperationAction.Resize,
            },
        };

        var result = _sut.Execute(items, Options(subfolderName: @"..\out"));

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("子資料夾名稱不安全");
    }

    private static ResizeOptions Options(string subfolderName = "resized") =>
        new(
            ResizeMode.Percentage,
            ScalePercent: 50,
            TargetWidth: 0,
            TargetHeight: 0,
            KeepAspectRatio: true,
            ResizeOutputMode.Subfolder,
            SubfolderName: subfolderName,
            ResamplerType.Bicubic);
}
