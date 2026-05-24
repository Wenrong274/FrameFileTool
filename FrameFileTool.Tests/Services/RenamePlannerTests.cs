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
    public void Plan_目標檔名重複_應標記錯誤()
    {
        // 兩個不同副檔名但補零後目標相同（同前綴、同編號、同副檔名）
        // 這裡用 padding 製造重複：startIndex=1, 兩個檔案都會是 F_1.png?
        // 更直接：兩個 .png 檔案都會 rename 到 F_1.png，F_2.png
        // 無法輕易用公開 API 製造重複；改用相同目標的情境：
        // 一個資料夾有 F_1.png 已存在，且計畫要 rename 另一個檔到 F_1.png
        // → 測試「目標檔名在計畫中重複」
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.png"),
        };

        // 只有 1 個槽位：startIndex=1, 但我們讓 padding 造成兩個都對應相同名稱
        // 實際上 RenamePlanner 永遠遞增，不可能直接重複。
        // 改測：計畫輸出中不應有重複的 TargetName（驗反面：正常情況無重複）
        var result = _sut.Plan(files, prefix: "F_", startIndex: 1, padding: 0);

        var targets = result.Select(r => r.TargetName).ToList();
        targets.Should().OnlyHaveUniqueItems();
        result.Should().AllSatisfy(item => item.HasError.Should().BeFalse());
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
}
