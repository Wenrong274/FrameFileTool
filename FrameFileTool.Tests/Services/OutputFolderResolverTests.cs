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

    [Fact]
    public void Resolve_自動改用子資料夾時Log應顯示原選擇與實際輸出()
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.75);

        var result = sut.ResolveForResize(@"C:\imgs\cloud", @"c:\IMGS\CLOUD", options);

        result.LogMessage.Should().Contain(@"c:\IMGS\CLOUD");
        result.LogMessage.Should().Contain(@"C:\imgs\cloud\cloud_x0.75");
    }

    [Fact]
    public void Resolve_絕對尺寸模式_應使用尺寸後綴()
    {
        var sut = new OutputFolderResolver();
        var options = AbsoluteOptions(width: 800, height: 600, keepAspectRatio: true);

        var result = sut.ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", options);

        result.TargetFolderPath.Should().Be(@"C:\imgs\cloud\cloud_800x600");
    }

    [Fact]
    public void Resolve_絕對尺寸單邊為零_應保留零值以反映使用者設定()
    {
        var sut = new OutputFolderResolver();
        var options = AbsoluteOptions(width: 800, height: 0, keepAspectRatio: true);

        var result = sut.ResolveForResize(@"C:\imgs\cloud", @"C:\imgs\cloud", options);

        result.TargetFolderPath.Should().Be(@"C:\imgs\cloud\cloud_800x0");
    }

    [Fact]
    public void Resolve_來源資料夾名稱含非法字元_應轉為安全資料夾名稱()
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.5);

        var result = sut.ResolveForResize(@"C:\imgs\cloud.", @"C:\imgs\cloud.", options);

        result.TargetFolderPath.Should().Be(@"C:\imgs\cloud.\cloud_x0.5");
    }

    [Fact]
    public void Resolve_來源路徑結尾為目前資料夾語意段_應以正規化後資料夾名稱建立子資料夾()
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.5);

        var result = sut.ResolveForResize(@"C:\imgs\.", @"C:\imgs", options);

        result.TargetFolderPath.Should().Be(@"C:\imgs\imgs_x0.5");
    }

    [Fact]
    public void Resolve_來源路徑結尾為上一層語意段_應以正規化後資料夾名稱建立子資料夾()
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.5);

        var result = sut.ResolveForResize(@"C:\imgs\..\root", @"C:\root", options);

        result.TargetFolderPath.Should().Be(@"C:\root\root_x0.5");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_來源資料夾為空_應保留使用者選擇的資料夾且不丟例外(string sourceFolder)
    {
        var sut = new OutputFolderResolver();
        var options = ScaleOptions(0.5);

        var result = sut.ResolveForResize(sourceFolder, @"D:\out", options);

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

    private static ResizeOptions AbsoluteOptions(int width, int height, bool keepAspectRatio) =>
        new(
            Mode: ResizeMode.Absolute,
            ScaleFactor: 1,
            TargetWidth: width,
            TargetHeight: height,
            KeepAspectRatio: keepAspectRatio,
            OutputMode: ResizeOutputMode.TargetFolder,
            TargetFolderPath: string.Empty,
            Resampler: ResamplerType.Bicubic);
}
