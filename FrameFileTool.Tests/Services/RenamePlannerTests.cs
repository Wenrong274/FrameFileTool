using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class RenamePlannerTests
{
    private readonly RenamePlanner _sut = new();

    private static FileItem MakeFile(string name, string folder = @"C:\imgs") =>
        new(Path.Combine(folder, name), folder, name, Path.GetExtension(name), 0);

    private static RenameOptions Renumber(
        string template,
        int startIndex = 0,
        RenameOutputMode outputMode = RenameOutputMode.RenameInPlace,
        string targetFolderPath = "") =>
        new(template, startIndex, UseOriginalNumber: false, outputMode, targetFolderPath);

    private static RenameOptions KeepNumber(
        string template,
        RenameOutputMode outputMode = RenameOutputMode.RenameInPlace,
        string targetFolderPath = "") =>
        new(template, StartIndex: 0, UseOriginalNumber: true, outputMode, targetFolderPath);

    // ---- 邊界案例 ----

    [Fact]
    public void Plan_空清單_應回傳空結果()
    {
        var result = _sut.Plan([], Renumber("F_[#]"));

        result.Should().BeEmpty();
    }

    // ---- 基本改名邏輯 ----

    [Fact]
    public void Plan_基本樣板與編號_應產生正確目標檔名()
    {
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.png"),
            MakeFile("c.png"),
        };

        var result = _sut.Plan(files, Renumber("F_[#]", startIndex: 1));

        result[0].TargetName.Should().Be("F_1.png");
        result[1].TargetName.Should().Be("F_2.png");
        result[2].TargetName.Should().Be("F_3.png");
    }

    [Fact]
    public void Plan_樣板補零位數_應正確填補()
    {
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.png"),
        };

        var result = _sut.Plan(files, Renumber("F_[####]", startIndex: 1));

        result[0].TargetName.Should().Be("F_0001.png");
        result[1].TargetName.Should().Be("F_0002.png");
    }

    [Fact]
    public void Plan_起始編號非零_應從指定編號開始()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Renumber("IMG_[#]", startIndex: 10));

        result[0].TargetName.Should().Be("IMG_10.png");
    }

    [Fact]
    public void Plan_起始編號預設為零_應從零開始編號()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Renumber("F_[###]"));

        result[0].TargetName.Should().Be("F_000.png");
    }

    // ---- 命名樣板 token 解析 ----

    [Fact]
    public void Plan_樣板token後方有文字_應保留為字面文字()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Renumber("Symbol_[###]_diffuse", startIndex: 7));

        result[0].TargetName.Should().Be("Symbol_007_diffuse.png");
    }

    [Fact]
    public void Plan_樣板含多個token_應各自套用自己的位數且填入同一個數字()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Renumber("[##]_x_[####]", startIndex: 5));

        result[0].TargetName.Should().Be("05_x_0005.png");
    }

    [Theory]
    [InlineData("Symbol_")]
    [InlineData("Symbol_[]")]
    [InlineData("Symbol_[#")]
    [InlineData("Symbol_#")]
    public void Plan_樣板缺少編號token_每個項目都應標記錯誤(string template)
    {
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.png"),
        };

        var result = _sut.Plan(files, Renumber(template));

        result.Should().AllSatisfy(item =>
        {
            item.ActionKind.Should().Be(OperationActionKind.Error);
            item.HasError.Should().BeTrue();
            item.Status.Should().Be("命名樣板缺少 [###] 編號欄位");
        });
    }

    [Fact]
    public void Plan_樣板含字面中括號與井號_應原樣輸出()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Renumber("Cut#2_[###]", startIndex: 3));

        result[0].TargetName.Should().Be("Cut#2_003.png");
    }

    // ---- 沿用原檔名編號 ----

    [Fact]
    public void Plan_沿用原編號_應取檔名最後一組連續數字()
    {
        var files = new[] { MakeFile("scene2_frame_0037.png") };

        var result = _sut.Plan(files, KeepNumber("Symbol_[###]"));

        result[0].TargetName.Should().Be("Symbol_0037.png");
    }

    [Fact]
    public void Plan_沿用原編號_應忽略樣板的補零位數()
    {
        var files = new[]
        {
            MakeFile("frame_0037.png"),
            MakeFile("frame_5.png"),
        };

        var result = _sut.Plan(files, KeepNumber("Symbol_[#]"));

        result[0].TargetName.Should().Be("Symbol_0037.png");
        result[1].TargetName.Should().Be("Symbol_5.png");
    }

    [Fact]
    public void Plan_沿用原編號_編號後方尾綴應原封不動保留()
    {
        var files = new[] { MakeFile("frame_0037_final.png") };

        var result = _sut.Plan(files, KeepNumber("Symbol_[###]"));

        result[0].TargetName.Should().Be("Symbol_0037_final.png");
    }

    [Fact]
    public void Plan_沿用原編號_樣板尾文字應排在原尾綴之前()
    {
        var files = new[] { MakeFile("frame_0037_final.png") };

        var result = _sut.Plan(files, KeepNumber("Symbol_[#]_v2"));

        result[0].TargetName.Should().Be("Symbol_0037_v2_final.png");
    }

    [Fact]
    public void Plan_沿用原編號_純數字檔名應正常處理()
    {
        var files = new[] { MakeFile("0037.png") };

        var result = _sut.Plan(files, KeepNumber("Symbol_[#]"));

        result[0].TargetName.Should().Be("Symbol_0037.png");
    }

    [Fact]
    public void Plan_沿用原編號_副檔名含數字不應被誤判為編號()
    {
        // 檔名主體無數字，含數字的只有副檔名 → 應視為無編號
        var files = new[] { MakeFile("clip.mp3") };

        var result = _sut.Plan(files, KeepNumber("Symbol_[#]"));

        result[0].ActionKind.Should().Be(OperationActionKind.Keep);
        result[0].Status.Should().Be("無編號，不處理");
        result[0].HasError.Should().BeFalse();
    }

    [Fact]
    public void Plan_沿用原編號_檔名無數字應標記不處理且不算錯誤()
    {
        var files = new[]
        {
            MakeFile("title.png"),
            MakeFile("frame_0001.png"),
        };

        var result = _sut.Plan(files, KeepNumber("Symbol_[#]"));

        result[0].ActionKind.Should().Be(OperationActionKind.Keep);
        result[0].Status.Should().Be("無編號，不處理");
        result[0].HasError.Should().BeFalse();

        // 無編號檔案不得阻擋其他檔案
        result[1].ActionKind.Should().Be(OperationActionKind.Rename);
        result[1].HasError.Should().BeFalse();
    }

    [Fact]
    public void Plan_沿用原編號_同編號不同尾綴不應撞名()
    {
        var files = new[]
        {
            MakeFile("char_0037_fg.png"),
            MakeFile("char_0037_bg.png"),
        };

        var result = _sut.Plan(files, KeepNumber("Symbol_[#]"));

        result[0].TargetName.Should().Be("Symbol_0037_fg.png");
        result[1].TargetName.Should().Be("Symbol_0037_bg.png");
        result.Should().AllSatisfy(item => item.HasError.Should().BeFalse());
    }

    [Fact]
    public void Plan_沿用原編號_跨資料夾複製同編號應偵測撞名()
    {
        var files = new[]
        {
            MakeFile("frame_0001.png", @"C:\imgs\A"),
            MakeFile("frame_0001.png", @"C:\imgs\B"),
        };

        var result = _sut.Plan(
            files,
            KeepNumber("Symbol_[#]", RenameOutputMode.CopyToTargetFolder, @"D:\out"));

        result[0].HasError.Should().BeFalse();
        result[1].HasError.Should().BeTrue();
        result[1].Status.Should().Be("目標檔名重複");
    }

    [Fact]
    public void Plan_沿用原編號_檔名與目標相同應標記保留不改名()
    {
        var files = new[] { MakeFile("Symbol_0037.png") };

        var result = _sut.Plan(files, KeepNumber("Symbol_[###]"));

        result[0].Action.Should().Be("保留");
        result[0].Status.Should().Be("檔名相同，不處理");
        result[0].HasError.Should().BeFalse();
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

        var result = _sut.Plan(files, Renumber("F_[#]", startIndex: 1));

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

        var result = _sut.Plan(files, Renumber("F_[#]", startIndex: 1), existingPaths);

        result[0].ActionKind.Should().Be(OperationActionKind.Rename);
        result[0].HasError.Should().BeTrue();
        result[0].Status.Should().Be("目標檔案已存在");
    }

    [Fact]
    public void Plan_原始檔名與目標相同_應標記保留不改名()
    {
        // 檔案本身就叫 F_1.png，改名計畫也是 F_1.png → 同名不處理
        var files = new[] { MakeFile("F_1.png") };

        var result = _sut.Plan(files, Renumber("F_[#]", startIndex: 1));

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
            Renumber("F_[#]", startIndex: 1, RenameOutputMode.CopyToTargetFolder, @"D:\out"));

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
            Renumber("F_[#]", startIndex: 1, RenameOutputMode.CopyToTargetFolder, targetFolderPath: ""));

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
            Renumber("F_[#]", startIndex: 1, RenameOutputMode.CopyToTargetFolder, @"D:\out"),
            existingPaths);

        result[0].ActionKind.Should().Be(OperationActionKind.Copy);
        result[0].HasError.Should().BeTrue();
        result[0].Status.Should().Be("目標檔案已存在");
    }

    [Theory]
    [InlineData(@"..\F_[#]")]
    [InlineData(@"nested\F_[#]")]
    [InlineData("nested/F_[#]")]
    [InlineData(@"C:\F_[#]")]
    public void Plan_樣板包含路徑語意_應標記錯誤(string template)
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Renumber(template, startIndex: 1));

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

        var result = _sut.Plan(files, Renumber("F_[#]", startIndex: 1));

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

        var result = _sut.Plan(files, Renumber("F_[#]"));

        result.Select(i => i.Index).Should().BeEquivalentTo([1, 2, 3, 4]);
    }

    // ---- ProjectTargetPaths ----

    [Fact]
    public void ProjectTargetPaths_空清單_應回傳空序列()
    {
        var result = _sut.ProjectTargetPaths(
            [], Renumber("F_[#]", targetFolderPath: @"C:\out"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void ProjectTargetPaths_單檔無補零_路徑應與Plan一致()
    {
        var files = new[] { MakeFile("a.png") };
        var options = Renumber("F_[#]", 0, RenameOutputMode.CopyToTargetFolder, @"C:\out");

        var projected = _sut.ProjectTargetPaths(files, options).ToList();
        var planned = _sut.Plan(files, options);

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
        var options = Renumber("Sym_[###]", 1, RenameOutputMode.CopyToTargetFolder, @"C:\out");

        var projected = _sut.ProjectTargetPaths(files, options).ToList();
        var planned = _sut.Plan(files, options);

        projected.Should().Equal(planned.Select(p => p.TargetPath));
    }

    [Fact]
    public void ProjectTargetPaths_沿用原編號_路徑應與Plan一致()
    {
        var files = new[]
        {
            MakeFile("frame_0037_final.png"),
            MakeFile("frame_12.png"),
        };
        var options = KeepNumber("Sym_[###]", RenameOutputMode.CopyToTargetFolder, @"C:\out");

        var projected = _sut.ProjectTargetPaths(files, options).ToList();
        var planned = _sut.Plan(files, options);

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

        var result = _sut.ProjectTargetPaths(
            files,
            Renumber("F_[#]", 0, RenameOutputMode.CopyToTargetFolder, @"C:\out")).ToList();

        // folderA: index 0 → F_0.png, index 1 → F_1.png；folderB: index 0 → F_0.png
        result.Should().Equal(
        [
            @"C:\out\F_0.png",
            @"C:\out\F_0.png",
            @"C:\out\F_1.png",
        ]);
    }
}
