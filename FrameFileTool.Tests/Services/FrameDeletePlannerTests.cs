using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class FrameDeletePlannerTests
{
    private readonly FrameDeletePlanner _sut = new();

    private static FileItem MakeFile(string name, string folder = @"C:\imgs") =>
        new(Path.Combine(folder, name), folder, name, Path.GetExtension(name), 0);

    // ---- 邊界案例 ----

    [Fact]
    public void Plan_空清單_應回傳空結果()
    {
        var result = _sut.Plan([], interval: 3);

        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Plan_間隔小於等於零_所有項目應標記為錯誤(int interval)
    {
        var files = new[] { MakeFile("a.png"), MakeFile("b.png") };

        var result = _sut.Plan(files, interval);

        result.Should().AllSatisfy(item =>
        {
            item.HasError.Should().BeTrue();
            item.Action.Should().Be("錯誤");
        });
    }

    // ---- 正常抽幀邏輯 ----

    [Fact]
    public void Plan_間隔3_每第三張應標記刪除()
    {
        var files = Enumerable.Range(1, 9)
            .Select(i => MakeFile($"frame{i}.png"))
            .ToList();

        var result = _sut.Plan(files, interval: 3);

        // 第 3、6、9 張（index 2、5、8）應被刪除
        result[2].Action.Should().Be("刪除");
        result[5].Action.Should().Be("刪除");
        result[8].Action.Should().Be("刪除");
    }

    [Fact]
    public void Plan_間隔3_非第三張應標記保留()
    {
        var files = Enumerable.Range(1, 5)
            .Select(i => MakeFile($"frame{i}.png"))
            .ToList();

        var result = _sut.Plan(files, interval: 3);

        result[0].Action.Should().Be("保留");
        result[1].Action.Should().Be("保留");
        result[3].Action.Should().Be("保留");
        result[4].Action.Should().Be("保留");
    }

    // ---- 子資料夾計數各自獨立 ----

    [Fact]
    public void Plan_包含子資料夾時_每個資料夾應各自計數()
    {
        var folderA = @"C:\imgs\A";
        var folderB = @"C:\imgs\B";

        var files = new[]
        {
            MakeFile("f1.png", folderA),
            MakeFile("f2.png", folderA),
            MakeFile("f3.png", folderA), // folderA 第 3 張 → 刪除
            MakeFile("f1.png", folderB),
            MakeFile("f2.png", folderB),
            MakeFile("f3.png", folderB), // folderB 第 3 張 → 刪除
        };

        var result = _sut.Plan(files, interval: 3);

        result[2].Action.Should().Be("刪除"); // folderA 第 3 張
        result[5].Action.Should().Be("刪除"); // folderB 第 3 張
        result[3].Action.Should().Be("保留"); // folderB 重從第 1 張計數
    }

    [Fact]
    public void Plan_結果序號應從1開始連續遞增()
    {
        var files = Enumerable.Range(1, 5)
            .Select(i => MakeFile($"f{i}.png"))
            .ToList();

        var result = _sut.Plan(files, interval: 3);

        result.Select(i => i.Index).Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }
}
