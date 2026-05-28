using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class ResizePlannerTests
{
    private readonly ResizePlanner _sut = new();

    private static FileItem MakeFile(string name, string folder = @"C:\imgs") =>
        new(Path.Combine(folder, name), folder, name, Path.GetExtension(name), 0);

    private static FileItem MakeFileWithDimensions(string name, int width, int height, string folder = @"C:\imgs") =>
        new(Path.Combine(folder, name), folder, name, Path.GetExtension(name), 0, width, height);

    private static ResizeOptions Percentage(
        int percent,
        ResizeOutputMode output = ResizeOutputMode.Subfolder,
        string subfolder = "resized",
        ResamplerType resampler = ResamplerType.Bicubic) =>
        new(ResizeMode.Percentage, percent, 0, 0, true, output, subfolder, resampler);

    private static ResizeOptions Absolute(
        int width,
        int height,
        bool keepAspect = true,
        ResizeOutputMode output = ResizeOutputMode.Subfolder,
        string subfolder = "resized") =>
        new(ResizeMode.Absolute, 0, width, height, keepAspect, output, subfolder, ResamplerType.Bicubic);

    // ── 邊界案例 ─────────────────────────────────────────────────

    [Fact]
    public void Plan_空清單_應回傳空結果()
    {
        var result = _sut.Plan([], Percentage(50));

        result.Should().BeEmpty();
    }

    // ── 百分比模式：參數驗證 ──────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Plan_百分比為零或負數_所有項目應標記錯誤(int percent)
    {
        var files = new[] { MakeFile("a.png"), MakeFile("b.png") };

        var result = _sut.Plan(files, Percentage(percent));

        result.Should().AllSatisfy(item =>
        {
            item.HasError.Should().BeTrue();
            item.Action.Should().Be(OperationAction.Error);
        });
    }

    [Fact]
    public void Plan_百分比超過上限10000_所有項目應標記錯誤()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Percentage(10001));

        result[0].HasError.Should().BeTrue();
        result[0].Action.Should().Be(OperationAction.Error);
    }

    [Fact]
    public void Plan_百分比50_應產生縮放計畫且Status含百分比資訊()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Percentage(50));

        result[0].HasError.Should().BeFalse();
        result[0].Action.Should().Be(OperationAction.Resize);
        result[0].Status.Should().Contain("50%");
    }

    [Fact]
    public void Plan_百分比200放大_應產生縮放計畫且Status含百分比資訊()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Percentage(200));

        result[0].HasError.Should().BeFalse();
        result[0].Action.Should().Be(OperationAction.Resize);
        result[0].Status.Should().Contain("200%");
    }

    // ── 絕對尺寸模式：參數驗證 ────────────────────────────────────

    [Fact]
    public void Plan_絕對模式寬高均為零_所有項目應標記錯誤()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Absolute(0, 0));

        result[0].HasError.Should().BeTrue();
        result[0].Action.Should().Be(OperationAction.Error);
    }

    [Fact]
    public void Plan_絕對模式維持比例且只指定高度_應為合法計畫()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Absolute(0, 480, keepAspect: true));

        result[0].HasError.Should().BeFalse();
        result[0].Action.Should().Be(OperationAction.Resize);
    }

    [Fact]
    public void Plan_絕對模式維持比例且只指定寬度_應為合法計畫()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Absolute(800, 0, keepAspect: true));

        result[0].HasError.Should().BeFalse();
        result[0].Action.Should().Be(OperationAction.Resize);
    }

    [Fact]
    public void Plan_絕對模式不維持比例且寬度為零_應標記錯誤()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Absolute(0, 480, keepAspect: false));

        result[0].HasError.Should().BeTrue();
        result[0].Action.Should().Be(OperationAction.Error);
    }

    [Fact]
    public void Plan_絕對模式不維持比例且高度為零_應標記錯誤()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Absolute(800, 0, keepAspect: false));

        result[0].HasError.Should().BeTrue();
        result[0].Action.Should().Be(OperationAction.Error);
    }

    [Fact]
    public void Plan_絕對模式800x600_Status應含尺寸資訊()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Absolute(800, 600));

        result[0].HasError.Should().BeFalse();
        result[0].Status.Should().Contain("800");
        result[0].Status.Should().Contain("600");
    }

    // ── 輸出路徑 ─────────────────────────────────────────────────

    [Fact]
    public void Plan_輸出模式覆寫_TargetName應與OriginalName相同()
    {
        var files = new[] { MakeFile("frame01.png") };

        var result = _sut.Plan(files, Percentage(50, output: ResizeOutputMode.Overwrite));

        result[0].TargetName.Should().Be("frame01.png");
    }

    [Fact]
    public void Plan_輸出模式子資料夾_TargetName應含子資料夾路徑()
    {
        var files = new[] { MakeFile("frame01.png") };

        var result = _sut.Plan(files, Percentage(50, output: ResizeOutputMode.Subfolder, subfolder: "resized"));

        result[0].TargetName.Should().Be(@"resized\frame01.png");
    }

    [Fact]
    public void Plan_子資料夾輸出目標檔已存在_應標記錯誤()
    {
        var folder = @"C:\imgs";
        var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(folder, "resized", "frame01.png"),
        };

        var files = new[] { MakeFile("frame01.png", folder) };

        var result = _sut.Plan(
            files,
            Percentage(50, output: ResizeOutputMode.Subfolder, subfolder: "resized"),
            existingPaths);

        result[0].HasError.Should().BeTrue();
        result[0].Action.Should().Be(OperationAction.Error);
        result[0].Status.Should().Contain("目標檔案已存在");
    }

    [Fact]
    public void Plan_子資料夾名稱為空字串_所有項目應標記錯誤()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Percentage(50, output: ResizeOutputMode.Subfolder, subfolder: ""));

        result[0].HasError.Should().BeTrue();
        result[0].Action.Should().Be(OperationAction.Error);
    }

    [Theory]
    [InlineData(@"..\out")]
    [InlineData(@"nested\out")]
    [InlineData("nested/out")]
    [InlineData(@"C:\out")]
    [InlineData(".")]
    [InlineData("..")]
    public void Plan_子資料夾名稱包含路徑語意_所有項目應標記錯誤(string subfolder)
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Percentage(50, output: ResizeOutputMode.Subfolder, subfolder: subfolder));

        result[0].HasError.Should().BeTrue();
        result[0].Action.Should().Be(OperationAction.Error);
        result[0].Status.Should().Contain("子資料夾名稱");
    }

    [Fact]
    public void Plan_輸出模式覆寫時子資料夾名稱不影響結果()
    {
        var files = new[] { MakeFile("a.png") };

        // 覆寫模式下 SubfolderName 為空也合法
        var result = _sut.Plan(files,
            new ResizeOptions(ResizeMode.Percentage, 50, 0, 0, true,
                ResizeOutputMode.Overwrite, "", ResamplerType.Bicubic));

        result[0].HasError.Should().BeFalse();
    }

    // ── 演算法欄位 ────────────────────────────────────────────────

    [Theory]
    [InlineData(ResamplerType.Bicubic, "一般用途")]
    [InlineData(ResamplerType.Lanczos3, "高品質縮小")]
    [InlineData(ResamplerType.CatmullRom, "高品質放大")]
    [InlineData(ResamplerType.NearestNeighbor, "像素精準")]
    [InlineData(ResamplerType.MitchellNetravali, "銳利優先")]
    public void Plan_各演算法_Status應含對應中文說明(ResamplerType resampler, string expectedHint)
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Percentage(50, resampler: resampler));

        result[0].Status.Should().Contain(expectedHint);
    }

    // ── 序號與結構 ────────────────────────────────────────────────

    [Fact]
    public void Plan_多個檔案_Index應從1開始連續遞增()
    {
        var files = Enumerable.Range(1, 4)
            .Select(i => MakeFile($"frame{i}.png"))
            .ToList();

        var result = _sut.Plan(files, Percentage(50));

        result.Select(i => i.Index).Should().BeEquivalentTo([1, 2, 3, 4]);
    }

    [Fact]
    public void Plan_合法設定_所有項目Action應為縮放且HasError為false()
    {
        var files = new[]
        {
            MakeFile("a.png"),
            MakeFile("b.jpg"),
            MakeFile("c.webp"),
        };

        var result = _sut.Plan(files, Percentage(75));

        result.Should().AllSatisfy(item =>
        {
            item.Action.Should().Be(OperationAction.Resize);
            item.HasError.Should().BeFalse();
        });
    }

    [Fact]
    public void Plan_合法設定_FullPath應與來源相同()
    {
        var files = new[] { MakeFile("a.png", @"C:\imgs") };

        var result = _sut.Plan(files, Percentage(50));

        result[0].FullPath.Should().Be(@"C:\imgs\a.png");
        result[0].OriginalName.Should().Be("a.png");
    }

    // ── 尺寸欄位 ─────────────────────────────────────────────────

    [Fact]
    public void Plan_來源無尺寸資訊_OriginalDimensions與TargetDimensions應為空字串()
    {
        var files = new[] { MakeFile("a.png") }; // Width=0, Height=0

        var result = _sut.Plan(files, Percentage(50));

        result[0].OriginalDimensions.Should().BeEmpty();
        result[0].TargetDimensions.Should().BeEmpty();
    }

    [Fact]
    public void Plan_百分比50且來源1920x1080_TargetDimensions應為960x540()
    {
        var files = new[] { MakeFileWithDimensions("a.png", 1920, 1080) };

        var result = _sut.Plan(files, Percentage(50));

        result[0].OriginalDimensions.Should().Be("1920×1080");
        result[0].TargetDimensions.Should().Be("960×540");
    }

    [Fact]
    public void Plan_百分比200且來源800x600_TargetDimensions應為1600x1200()
    {
        var files = new[] { MakeFileWithDimensions("a.png", 800, 600) };

        var result = _sut.Plan(files, Percentage(200));

        result[0].OriginalDimensions.Should().Be("800×600");
        result[0].TargetDimensions.Should().Be("1600×1200");
    }

    [Fact]
    public void Plan_絕對模式不維持比例且來源1920x1080_TargetDimensions應為指定值()
    {
        var files = new[] { MakeFileWithDimensions("a.png", 1920, 1080) };

        var result = _sut.Plan(files, Absolute(800, 600, keepAspect: false));

        result[0].TargetDimensions.Should().Be("800×600");
    }

    [Fact]
    public void Plan_絕對模式維持比例只指定寬度_TargetDimensions應依比例計算高度()
    {
        // 原始 1920×1080（16:9），指定寬 960 → 高應為 540
        var files = new[] { MakeFileWithDimensions("a.png", 1920, 1080) };

        var result = _sut.Plan(files, Absolute(960, 0, keepAspect: true));

        result[0].TargetDimensions.Should().Be("960×540");
    }

    [Fact]
    public void Plan_絕對模式維持比例只指定高度_TargetDimensions應依比例計算寬度()
    {
        // 原始 1920×1080（16:9），指定高 540 → 寬應為 960
        var files = new[] { MakeFileWithDimensions("a.png", 1920, 1080) };

        var result = _sut.Plan(files, Absolute(0, 540, keepAspect: true));

        result[0].TargetDimensions.Should().Be("960×540");
    }

    [Fact]
    public void Plan_絕對模式維持比例兩邊均指定_TargetDimensions應fit在框內()
    {
        // 原始 1920×1080（16:9），框 800×800 → scale = min(800/1920, 800/1080)
        // = min(0.4167, 0.7407) = 0.4167 → 800×450
        var files = new[] { MakeFileWithDimensions("a.png", 1920, 1080) };

        var result = _sut.Plan(files, Absolute(800, 800, keepAspect: true));

        result[0].TargetDimensions.Should().Be("800×450");
    }

    [Fact]
    public void Plan_有錯誤的項目_OriginalDimensions與TargetDimensions均應為空字串()
    {
        var files = new[] { MakeFileWithDimensions("a.png", 1920, 1080) };

        // 百分比為 0 → 全部標記為錯誤
        var result = _sut.Plan(files, Percentage(0));

        result[0].HasError.Should().BeTrue();
        result[0].OriginalDimensions.Should().BeEmpty();
        result[0].TargetDimensions.Should().BeEmpty();
    }
}
