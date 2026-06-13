using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class OutputFolderResolverTests
{
    [Fact]
    public void Resolve_來源與目標不同_應保留使用者選擇的資料夾()
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.5);

        var result = sut.ResolveForResize(@"C:\imgs\cloud", @"D:\out", options);

        result.TargetFolderPath.Should().Be(@"D:\out");
        result.WasAutoRedirected.Should().BeFalse();
        result.LogMessage.Should().BeNull();
    }

    private static ResizeOptions ScaleOptions(double factor) =>
        new(
            Mode: ResizeMode.ScaleFactor,
            ScaleFactor: factor,
            TargetWidth: 0,
            TargetHeight: 0,
            KeepAspectRatio: true,
            OutputMode: ResizeOutputMode.TargetFolder,
            TargetFolderPath: string.Empty,
            Resampler: ResamplerType.Bicubic);
}
