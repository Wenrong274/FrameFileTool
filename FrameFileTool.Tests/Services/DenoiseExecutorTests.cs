using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;
using ImageMagick;

namespace FrameFileTool.Tests.Services;

public sealed class DenoiseExecutorTests
{
    private readonly DenoiseExecutor _sut = new();

    private static DenoiseOptions Options(DenoiseMode mode = DenoiseMode.Standard) => new(mode);

    [Fact]
    public async Task ExecuteAsync_取消權杖已取消_應回傳取消結果並略過項目()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var items = new[]
        {
            new OperationPreviewItem
            {
                FullPath = @"C:\imgs\a.png",
                OriginalName = "a.png",
                ActionKind = OperationActionKind.Denoise,
            },
        };

        var result = await _sut.ExecuteAsync(items, Options(), cancellationToken: cts.Token);

        result.Canceled.Should().BeTrue();
        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_取消勾選項目_應略過且不回報進度()
    {
        var items = new[]
        {
            new OperationPreviewItem
            {
                FullPath = @"C:\imgs\a.png",
                OriginalName = "a.png",
                ActionKind = OperationActionKind.Denoise,
                IsIncluded = false,
            },
        };
        var reports = new List<ResizeProgressReport>();
        var progress = new Progress<ResizeProgressReport>(reports.Add);

        var result = await _sut.ExecuteAsync(items, Options(), progress);

        result.Canceled.Should().BeFalse();
        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
        reports.Should().BeEmpty();
    }

    [Fact]
    public void Execute_非Denoise動作項目_應略過()
    {
        var items = new[]
        {
            new OperationPreviewItem
            {
                FullPath = @"C:\imgs\a.png",
                OriginalName = "a.png",
                ActionKind = OperationActionKind.Resize,
            },
        };

        var result = _sut.Execute(items, Options());

        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Execute_Off模式_應拒絕執行()
    {
        var items = new[]
        {
            new OperationPreviewItem
            {
                FullPath = @"C:\imgs\a.png",
                OriginalName = "a.png",
                ActionKind = OperationActionKind.Denoise,
            },
        };

        var result = _sut.Execute(items, Options(DenoiseMode.Off));

        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("未選擇降噪模式");
    }

    [Fact]
    public void Execute_來源檔案不存在_應列入錯誤明細且不中斷其餘檔案()
    {
        var root = CreateTestRoot();

        try
        {
            var existingPath = Path.Combine(root, "exists.png");
            WriteNoisyImage(existingPath, 64, 64);

            var items = new[]
            {
                new OperationPreviewItem
                {
                    FullPath = Path.Combine(root, "missing.png"),
                    OriginalName = "missing.png",
                    ActionKind = OperationActionKind.Denoise,
                },
                new OperationPreviewItem
                {
                    FullPath = existingPath,
                    OriginalName = "exists.png",
                    ActionKind = OperationActionKind.Denoise,
                },
            };

            var result = _sut.Execute(items, Options());

            result.SuccessCount.Should().Be(1);
            result.Errors.Should().ContainSingle()
                .Which.Should().Contain("missing.png").And.Contain("來源檔案不存在");
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Theory]
    [InlineData(DenoiseMode.Standard)]
    [InlineData(DenoiseMode.Strong)]
    public void Execute_Standard和Strong模式_應覆寫原檔且明顯降低像素顆粒(DenoiseMode mode)
    {
        var root = CreateTestRoot();

        try
        {
            var sourcePath = Path.Combine(root, "noise.png");
            WriteNoisyImage(sourcePath, 64, 64);

            byte[] originalPixels;
            double originalRoughness;
            using (var original = new MagickImage(sourcePath))
            {
                originalPixels = original.GetPixels().ToByteArray("RGB")!;
                originalRoughness = MeanAdjacentLumaDifference(original);
            }

            var item = new OperationPreviewItem
            {
                FullPath = sourcePath,
                OriginalName = "noise.png",
                ActionKind = OperationActionKind.Denoise,
            };

            var result = _sut.Execute([item], Options(mode));

            result.SuccessCount.Should().Be(1);
            result.Errors.Should().BeEmpty();

            using var denoised = new MagickImage(sourcePath);
            denoised.Width.Should().Be(64);
            denoised.Height.Should().Be(64);
            denoised.GetPixels().ToByteArray("RGB").Should().NotEqual(originalPixels);

            var denoisedRoughness = MeanAdjacentLumaDifference(denoised);
            denoisedRoughness.Should().BeLessThan(originalRoughness * 0.75);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void Execute_Detail模式_應覆寫原檔且不報錯()
    {
        var root = CreateTestRoot();

        try
        {
            var sourcePath = Path.Combine(root, "noise.png");
            WriteNoisyImage(sourcePath, 64, 64);

            var item = new OperationPreviewItem
            {
                FullPath = sourcePath,
                OriginalName = "noise.png",
                ActionKind = OperationActionKind.Denoise,
            };

            var result = _sut.Execute([item], Options(DenoiseMode.Detail));

            result.SuccessCount.Should().Be(1);
            result.Errors.Should().BeEmpty();

            using var denoised = new MagickImage(sourcePath);
            denoised.Width.Should().Be(64);
            denoised.Height.Should().Be(64);
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    [Fact]
    public void Execute_執行後_不應留下暫存檔()
    {
        var root = CreateTestRoot();

        try
        {
            var sourcePath = Path.Combine(root, "noise.png");
            WriteNoisyImage(sourcePath, 16, 16);

            var item = new OperationPreviewItem
            {
                FullPath = sourcePath,
                OriginalName = "noise.png",
                ActionKind = OperationActionKind.Denoise,
            };

            _sut.Execute([item], Options());

            Directory.GetFiles(root, "*.tmp").Should().BeEmpty();
        }
        finally
        {
            DeleteTestRoot(root);
        }
    }

    // ── 測試輔助 ─────────────────────────────────────────────────

    private static string CreateTestRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "FrameFileToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTestRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void WriteNoisyImage(string path, uint width, uint height)
    {
        using var image = CreateNoisyImage(width, height);
        image.Write(path);
    }

    private static MagickImage CreateNoisyImage(uint width, uint height)
    {
        var image = new MagickImage(MagickColors.White, width, height);
        var random = new Random(274);
        using var pixels = image.GetPixels();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var value = (byte)random.Next(byte.MinValue, byte.MaxValue + 1);
                pixels.SetPixel(x, y, [value, (byte)(255 - value), (byte)((value + 73) % 256)]);
            }
        }

        return image;
    }

    private static double MeanAdjacentLumaDifference(MagickImage image)
    {
        var pixels = image.GetPixels().ToByteArray("RGB")
            ?? throw new InvalidOperationException("無法讀取測試圖片像素資料。");

        var width = (int)image.Width;
        var height = (int)image.Height;
        double sum = 0;
        long count = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width + x) * 3;
                var current = Luma(pixels[index], pixels[index + 1], pixels[index + 2]);

                if (x + 1 < width)
                {
                    var right = index + 3;
                    sum += Math.Abs(current - Luma(pixels[right], pixels[right + 1], pixels[right + 2]));
                    count++;
                }

                if (y + 1 < height)
                {
                    var bottom = ((y + 1) * width + x) * 3;
                    sum += Math.Abs(current - Luma(pixels[bottom], pixels[bottom + 1], pixels[bottom + 2]));
                    count++;
                }
            }
        }

        return sum / count;
    }

    private static double Luma(byte red, byte green, byte blue) =>
        (0.2126 * red) + (0.7152 * green) + (0.0722 * blue);
}
