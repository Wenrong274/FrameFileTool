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

    [Fact]
    public void Resolve_來源與目標相同_倍率模式應改用來源內倍率後綴資料夾()
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.5);

        var result = sut.ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", options);

        result.TargetFolderPath.Should().Be(@"C:\imgs\cloud\cloud_x0.5");
        result.WasAutoRedirected.Should().BeTrue();
        result.LogMessage.Should().Contain(@"C:\imgs\cloud\cloud_x0.5");
    }

    [Fact]
    public void Resolve_來源與目標大小寫不同但相同_應改用來源內子資料夾()
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.75);

        var result = sut.ResolveForResize(@"C:\imgs\cloud", @"c:\IMGS\CLOUD", options);

        result.TargetFolderPath.Should().Be(@"C:\imgs\cloud\cloud_x0.75");
        result.WasAutoRedirected.Should().BeTrue();
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
