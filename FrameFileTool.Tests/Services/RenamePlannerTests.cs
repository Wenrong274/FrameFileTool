using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class RenamePlannerTests
{
    private readonly RenamePlanner _sut = new();

    private static FileItem MakeFile(string name, string folder = @"C:\imgs") =>
        new(Path.Combine(folder, name), folder, name, Path.GetExtension(name), 0);

    // ---- 邊界案例 ----

    [Fact]
    public void Plan_空清單_應回傳空結果()
    {
        var result = _sut.Plan([], prefix: "F_", startIndex: 0, padding: 0);

        result.Should().BeEmpty();
    }

    // ---- 基本改名邏輯 ----

    [Fact]
    public void Plan_基本前綴與編號_應產生正確目標檔名()
    {
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.png"),
            MakeFile("c.png"),
        };

        var result = _sut.Plan(files, prefix: "F_", startIndex: 1, padding: 0);

        result[0].TargetName.Should().Be("F_1.png");
        result[1].TargetName.Should().Be("F_2.png");
        result[2].TargetName.Should().Be("F_3.png");
    }

    [Fact]
    public void Plan_補零位數_應正確填補()
    {
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.png"),
        };

        var result = _sut.Plan(files, prefix: "F_", startIndex: 1, padding: 4);

        result[0].TargetName.Should().Be("F_0001.png");
        result[1].TargetName.Should().Be("F_0002.png");
    }

    [Fact]
    public void Plan_起始編號非零_應從指定編號開始()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, prefix: "IMG_", startIndex: 10, padding: 0);

        result[0].TargetName.Should().Be("IMG_10.png");
    }

    // ---- 衝突偵測 ----

    [Fact]
    public void Plan_正常改名_目標檔名不應重複()
    {
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.png"),
        };

        var result = _sut.Plan(files, prefix: "F_", startIndex: 1, padding: 0);

        var targets = result.Select(r => r.TargetName).ToList();
        targets.Should().OnlyHaveUniqueItems();
        result.Should().AllSatisfy(item => item.HasError.Should().BeFalse());
    }

    [Fact]
    public void Plan_目標檔案已存在且不在來源清單_應標記錯誤()
    {
        var files = new[] { MakeFile("a.png") };
        var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\imgs\F_1.png",
        };

        var result = _sut.Plan(files, prefix: "F_", startIndex: 1, padding: 0, existingPaths);

        result[0].ActionKind.Should().Be(OperationActionKind.Rename);
        result[0].HasError.Should().BeTrue();
        result[0].Status.Should().Be("目標檔案已存在");
    }

    [Fact]
    public void Plan_原始檔名與目標相同_應標記保留不改名()
    {
        // 檔案本身就叫 F_1.png，改名計畫也是 F_1.png → 同名不處理
        var files = new[] { MakeFile("F_1.png") };

        var result = _sut.Plan(files, prefix: "F_", startIndex: 1, padding: 0);

        result[0].Action.Should().Be("保留");
        result[0].Status.Should().Be("檔名相同，不處理");
        result[0].HasError.Should().BeFalse();
    }

    [Fact]
    public void Plan_複製改名到指定資料夾_應設定完整目標路徑()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(
            files,
            prefix: "F_",
            startIndex: 1,
            padding: 0,
            existingPaths: null,
            outputMode: RenameOutputMode.CopyToTargetFolder,
            targetFolderPath: @"D:\out");

        result[0].ActionKind.Should().Be(OperationActionKind.Copy);
        result[0].TargetName.Should().Be(@"D:\out\F_1.png");
        result[0].TargetPath.Should().Be(@"D:\out\F_1.png");
        result[0].Status.Should().Be("可複製並改名");
    }

    [Fact]
    public void Plan_複製改名未指定資料夾_應標示執行時選擇資料夾()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(
            files,
            prefix: "F_",
            startIndex: 1,
            padding: 0,
            existingPaths: null,
            outputMode: RenameOutputMode.CopyToTargetFolder,
            targetFolderPath: "");

        result[0].ActionKind.Should().Be(OperationActionKind.Copy);
        result[0].HasError.Should().BeFalse();
        result[0].TargetName.Should().Be("執行時選擇資料夾");
        result[0].TargetPath.Should().BeEmpty();
    }

    [Fact]
    public void Plan_複製改名目標檔已存在_應標記錯誤()
    {
        var files = new[] { MakeFile("a.png") };
        var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"D:\out\F_1.png",
        };

        var result = _sut.Plan(
            files,
            prefix: "F_",
            startIndex: 1,
            padding: 0,
            existingPaths: existingPaths,
            outputMode: RenameOutputMode.CopyToTargetFolder,
            targetFolderPath: @"D:\out");

        result[0].ActionKind.Should().Be(OperationActionKind.Copy);
        result[0].HasError.Should().BeTrue();
        result[0].Status.Should().Be("目標檔案已存在");
    }

    [Theory]
    [InlineData(@"..\F_")]
    [InlineData(@"nested\F_")]
    [InlineData("nested/F_")]
    [InlineData(@"C:\F_")]
    public void Plan_前綴包含路徑語意_應標記錯誤(string prefix)
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, prefix: prefix, startIndex: 1, padding: 0);

        result[0].ActionKind.Should().Be(OperationActionKind.Error);
        result[0].HasError.Should().BeTrue();
        result[0].Status.Should().Contain("目標檔名");
    }

    // ---- 子資料夾各自計數 ----

    [Fact]
    public void Plan_包含子資料夾_每個資料夾編號應各自從startIndex開始()
    {
        var folderA = @"C:\imgs\A";
        var folderB = @"C:\imgs\B";

        var files = new[]
        {
            MakeFile("a1.png", folderA),
            MakeFile("a2.png", folderA),
            MakeFile("b1.png", folderB),
            MakeFile("b2.png", folderB),
        };

        var result = _sut.Plan(files, prefix: "F_", startIndex: 1, padding: 0);

        // folderA：F_1.png, F_2.png
        result[0].TargetName.Should().Be("F_1.png");
        result[1].TargetName.Should().Be("F_2.png");

        // folderB：也從 F_1.png 重新開始
        result[2].TargetName.Should().Be("F_1.png");
        result[3].TargetName.Should().Be("F_2.png");
    }

    [Fact]
    public void Plan_所有項目序號應從1開始連續遞增()
    {
        var files = Enumerable.Range(1, 4)
            .Select(i => MakeFile($"f{i}.png"))
            .ToList();

        var result = _sut.Plan(files, prefix: "F_", startIndex: 0, padding: 0);

        result.Select(i => i.Index).Should().BeEquivalentTo([1, 2, 3, 4]);
    }

    // ---- ProjectTargetPaths ----

    [Fact]
    public void ProjectTargetPaths_空清單_應回傳空序列()
    {
        var result = _sut.ProjectTargetPaths([], prefix: "F_", startIndex: 0, padding: 0, targetFolderPath: @"C:\out");

        result.Should().BeEmpty();
    }

    [Fact]
    public void ProjectTargetPaths_單檔無補零_路徑應與Plan一致()
    {
        var files = new[] { MakeFile("a.png") };

        var projected = _sut.ProjectTargetPaths(files, "F_", startIndex: 0, padding: 0, @"C:\out").ToList();
        var planned = _sut.Plan(files, "F_", startIndex: 0, padding: 0,
            outputMode: RenameOutputMode.CopyToTargetFolder, targetFolderPath: @"C:\out");

        projected.Should().ContainSingle().Which.Should().Be(planned[0].TargetPath);
    }

    [Fact]
    public void ProjectTargetPaths_多檔有補零_路徑應與Plan一致()
    {
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.png"),
            MakeFile("c.png"),
        };

        var projected = _sut.ProjectTargetPaths(files, "Sym_", startIndex: 1, padding: 3, @"C:\out").ToList();
        var planned = _sut.Plan(files, "Sym_", startIndex: 1, padding: 3,
            outputMode: RenameOutputMode.CopyToTargetFolder, targetFolderPath: @"C:\out");

        projected.Should().Equal(planned.Select(p => p.TargetPath));
    }

    [Fact]
    public void ProjectTargetPaths_多個子資料夾_每個資料夾各自從startIndex計數()
    {
        var files = new[]
        {
            MakeFile("a.png", @"C:\imgs\folderA"),
            MakeFile("b.png", @"C:\imgs\folderB"),
            MakeFile("c.png", @"C:\imgs\folderA"),
        };

        var result = _sut.ProjectTargetPaths(files, "F_", startIndex: 0, padding: 0, @"C:\out").ToList();

        // folderA: index 0 → F_0.png, index 1 → F_1.png；folderB: index 0 → F_0.png
        result.Should().Equal(
        [
            @"C:\out\F_0.png",
            @"C:\out\F_0.png",
            @"C:\out\F_1.png",
        ]);
    }
}
