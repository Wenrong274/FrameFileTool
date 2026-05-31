using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;
using ImageMagick;

namespace FrameFileTool.Tests.Services;

public sealed class DenoisePreviewServiceTests
{
    private readonly DenoisePreviewService _sut = new();

    // ── 來源無法讀取 ──────────────────────────────────────────

    [Fact]
    public async Task GeneratePreviewAsync_來源不存在_回傳含錯誤訊息的結果()
    {
        var result = await _sut.GeneratePreviewAsync(
            @"C:\non_existent_file.png",
            [DenoiseMode.Standard]);

        result.HasError.Should().BeTrue();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ── 正常路徑 ─────────────────────────────────────────────

    [Fact]
    public async Task GeneratePreviewAsync_來源有效_回傳各模式的PNG位元組()
    {
        var root = Path.Combine(Path.GetTempPath(), "FrameFileToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = CreateTestImage(root, "source.png", 64, 64);
            var modes = new[] { DenoiseMode.Detail, DenoiseMode.Standard, DenoiseMode.Strong };

            var result = await _sut.GeneratePreviewAsync(sourcePath, modes);

            result.HasError.Should().BeFalse();
            result.PreviewsByMode.Should().ContainKeys(modes);
            foreach (var key in modes)
            {
                result.PreviewsByMode[key].Should().NotBeEmpty();
            }
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GeneratePreviewAsync_大圖_裁切後尺寸不超過256()
    {
        var root = Path.Combine(Path.GetTempPath(), "FrameFileToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = CreateTestImage(root, "large.png", 512, 512);

            var result = await _sut.GeneratePreviewAsync(sourcePath, [DenoiseMode.Off]);

            result.HasError.Should().BeFalse();
            using var preview = new MagickImage(result.PreviewsByMode[DenoiseMode.Off]);
            preview.Width.Should().BeLessThanOrEqualTo(256);
            preview.Height.Should().BeLessThanOrEqualTo(256);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GeneratePreviewAsync_小圖_裁切後尺寸等於原始尺寸()
    {
        var root = Path.Combine(Path.GetTempPath(), "FrameFileToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = CreateTestImage(root, "small.png", 32, 32);

            var result = await _sut.GeneratePreviewAsync(sourcePath, [DenoiseMode.Off]);

            result.HasError.Should().BeFalse();
            using var preview = new MagickImage(result.PreviewsByMode[DenoiseMode.Off]);
            preview.Width.Should().Be(32);
            preview.Height.Should().Be(32);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GeneratePreviewAsync_模式順序穩定_多次呼叫回傳相同鍵集合()
    {
        var root = Path.Combine(Path.GetTempPath(), "FrameFileToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = CreateTestImage(root, "source.png", 64, 64);
            var modes = new[] { DenoiseMode.Detail, DenoiseMode.Standard, DenoiseMode.Strong };

            var result1 = await _sut.GeneratePreviewAsync(sourcePath, modes);
            var result2 = await _sut.GeneratePreviewAsync(sourcePath, modes);

            result1.PreviewsByMode.Keys.Should().BeEquivalentTo(result2.PreviewsByMode.Keys);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    // ── Standard 與 Strong 模式確實改變像素 ─────────────────────

    [Theory]
    [InlineData(DenoiseMode.Standard)]
    [InlineData(DenoiseMode.Strong)]
    public async Task GeneratePreviewAsync_Standard和Strong模式與Off模式_像素應不同(DenoiseMode mode)
    {
        var root = Path.Combine(Path.GetTempPath(), "FrameFileToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = CreateNoisyTestImage(root, "noisy.png", 64, 64);
            var modes = new[] { DenoiseMode.Off, mode };

            var result = await _sut.GeneratePreviewAsync(sourcePath, modes);

            result.HasError.Should().BeFalse();
            result.PreviewsByMode[DenoiseMode.Off]
                .Should().NotEqual(result.PreviewsByMode[mode]);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    // ── 輔助 ──────────────────────────────────────────────────

    private static string CreateTestImage(string root, string name, uint width, uint height)
    {
        var path = Path.Combine(root, name);
        using var image = new MagickImage(MagickColors.Gray, width, height);
        image.Write(path);
        return path;
    }

    private static string CreateNoisyTestImage(string root, string name, uint width, uint height)
    {
        var path = Path.Combine(root, name);
        using var image = new MagickImage(MagickColors.White, width, height);
        var random = new Random(42);
        using var pixels = image.GetPixels();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var value = (byte)random.Next(0, 256);
                pixels.SetPixel(x, y, [value, (byte)(255 - value), (byte)((value + 73) % 256)]);
            }
        }

        image.Write(path);
        return path;
    }
}
