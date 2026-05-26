using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class ResizePlannerTests
{
    private readonly ResizePlanner _sut = new();

    private static FileItem MakeFile(string name, string folder = @"C:\imgs") =>
        new(Path.Combine(folder, name), folder, name, Path.GetExtension(name), 0);

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
    public void Plan_子資料夾名稱為空字串_所有項目應標記錯誤()
    {
        var files = new[] { MakeFile("a.png") };

        var result = _sut.Plan(files, Percentage(50, output: ResizeOutputMode.Subfolder, subfolder: ""));

        result[0].HasError.Should().BeTrue();
        result[0].Action.Should().Be(OperationAction.Error);
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
}
