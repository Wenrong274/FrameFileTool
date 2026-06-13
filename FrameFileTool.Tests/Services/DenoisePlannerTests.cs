using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class DenoisePlannerTests
{
    private readonly DenoisePlanner _sut = new();

    private static FileItem File(string name, string dir = @"C:\imgs") =>
        new(
            FullPath: Path.Combine(dir, name),
            DirectoryPath: dir,
            Name: name,
            Extension: Path.GetExtension(name),
            SizeBytes: 100);

    [Fact]
    public void Plan_空檔案清單_應回傳空結果()
    {
        var result = _sut.Plan([], new DenoiseOptions(DenoiseMode.Standard));

        result.Should().BeEmpty();
    }

    [Fact]
    public void Plan_標準模式_所有項目應為Denoise且覆寫原檔()
    {
        var files = new[] { File("a.png"), File("b.png") };

        var result = _sut.Plan(files, new DenoiseOptions(DenoiseMode.Standard));

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(item =>
        {
            item.ActionKind.Should().Be(OperationActionKind.Denoise);
            item.HasError.Should().BeFalse();
            item.IsIncluded.Should().BeTrue();
        });

        // 覆寫原檔：目標名 = 原名、目標路徑 = 來源路徑
        result[0].TargetName.Should().Be("a.png");
        result[0].TargetPath.Should().Be(@"C:\imgs\a.png");
        result[1].Index.Should().Be(2);
    }

    [Theory]
    [InlineData(DenoiseMode.Detail, "保留細節降噪")]
    [InlineData(DenoiseMode.Standard, "標準降噪")]
    [InlineData(DenoiseMode.Strong, "強力降噪")]
    public void Plan_各降噪模式_Status應含對應模式名稱(DenoiseMode mode, string expectedText)
    {
        var result = _sut.Plan([File("a.png")], new DenoiseOptions(mode));

        result.Should().ContainSingle()
            .Which.Status.Should().Contain(expectedText).And.Contain("覆寫原檔");
    }

    [Fact]
    public void Plan_Off模式_所有項目應標記錯誤()
    {
        var files = new[] { File("a.png"), File("b.png") };

        var result = _sut.Plan(files, new DenoiseOptions(DenoiseMode.Off));

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(item =>
        {
            item.ActionKind.Should().Be(OperationActionKind.Error);
            item.HasError.Should().BeTrue();
            item.IsIncluded.Should().BeFalse();
            item.Status.Should().Be("未選擇降噪模式");
        });
    }
}
