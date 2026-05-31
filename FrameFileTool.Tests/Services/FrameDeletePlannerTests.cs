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

    [Fact]
    public void Plan_複製保留幀到指定資料夾_保留幀應標記複製並設定目標路徑()
    {
        var targetFolder = @"D:\out";
        var files = Enumerable.Range(1, 4)
            .Select(i => MakeFile($"frame{i}.png"))
            .ToList();

        var result = _sut.Plan(
            files,
            interval: 3,
            outputMode: FrameDeleteOutputMode.CopyKeptToTargetFolder,
            targetFolderPath: targetFolder);

        result[0].ActionKind.Should().Be(OperationActionKind.Copy);
        result[0].TargetName.Should().Be(@"D:\out\frame1.png");
        result[0].TargetPath.Should().Be(@"D:\out\frame1.png");
        result[1].ActionKind.Should().Be(OperationActionKind.Copy);
        result[2].ActionKind.Should().Be(OperationActionKind.Keep);
        result[2].Status.Should().Contain("不輸出");
        result[3].ActionKind.Should().Be(OperationActionKind.Copy);
    }

    [Fact]
    public void Plan_複製模式未指定資料夾_應標示執行時選擇資料夾()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(
            files,
            interval: 3,
            outputMode: FrameDeleteOutputMode.CopyKeptToTargetFolder,
            targetFolderPath: "");

        result[0].ActionKind.Should().Be(OperationActionKind.Copy);
        result[0].HasError.Should().BeFalse();
        result[0].TargetName.Should().Be("執行時選擇資料夾");
        result[0].TargetPath.Should().BeEmpty();
    }

    [Fact]
    public void Plan_指定資料夾目標檔已存在_應標記錯誤()
    {
        var targetFolder = @"D:\out";
        var files = new[] { MakeFile("a.png") };
        var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"D:\out\a.png",
        };

        var result = _sut.Plan(
            files,
            interval: 3,
            outputMode: FrameDeleteOutputMode.CopyKeptToTargetFolder,
            targetFolderPath: targetFolder,
            existingPaths: existingPaths);

        result[0].ActionKind.Should().Be(OperationActionKind.Error);
        result[0].HasError.Should().BeTrue();
        result[0].Status.Should().Be("目標檔案已存在");
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
