using FluentAssertions;
using FrameFileTool.Models;
using FrameFileTool.Services;
using ImageMagick;

namespace FrameFileTool.Tests.Services;

public sealed class ImageResizeExecutorTests
{
    private readonly ImageResizeExecutor _sut = new();

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
                ActionKind = OperationActionKind.Resize,
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
                ActionKind = OperationActionKind.Resize,
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
    public void Execute_指定資料夾路徑空白_應拒絕執行()
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

        var result = _sut.Execute(items, Options(outputMode: ResizeOutputMode.TargetFolder, targetFolderPath: ""));

        result.SuccessCount.Should().Be(0);
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("目標資料夾路徑不安全");
    }

    [Fact]
    public void Execute_取消勾選項目_應略過且不驗證目標資料夾()
    {
        var items = new[]
        {
            new OperationPreviewItem
            {
                FullPath = @"C:\imgs\a.png",
                OriginalName = "a.png",
                ActionKind = OperationActionKind.Resize,
                IsIncluded = false,
            },
        };

        var result = _sut.Execute(items, Options(outputMode: ResizeOutputMode.TargetFolder, targetFolderPath: ""));

        result.SuccessCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Execute_指定資料夾不存在_應自動建立並輸出一半尺寸()
    {
        var root = Path.Combine(Path.GetTempPath(), "FrameFileToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "a.png");
            using (var image = new MagickImage(MagickColors.White, 2, 2))
            {
                image.Write(sourcePath);
            }

            var item = new OperationPreviewItem
            {
                FullPath = sourcePath,
                OriginalName = "a.png",
                ActionKind = OperationActionKind.Resize,
            };

            var targetPath = Path.Combine(root, "out", "a.png");
            var result = _sut.Execute([item], Options(factor: 0.5, targetFolderPath: Path.Combine(root, "out")));

            result.SuccessCount.Should().Be(1);
            result.Errors.Should().BeEmpty();
            File.Exists(targetPath).Should().BeTrue();

            using var resized = new MagickImage(targetPath);
            resized.Width.Should().Be(1);
            resized.Height.Should().Be(1);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Execute_啟用降噪_應輸出相同尺寸且像素內容改變()
    {
        var root = Path.Combine(Path.GetTempPath(), "FrameFileToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var sourcePath = Path.Combine(root, "noise.png");
            using (var image = CreateNoisyImage(64, 64))
            {
                image.Write(sourcePath);
            }

            var item = new OperationPreviewItem
            {
                FullPath = sourcePath,
                OriginalName = "noise.png",
                ActionKind = OperationActionKind.Resize,
            };

            var plainOut = Path.Combine(root, "plain");
            var denoiseOut = Path.Combine(root, "denoise");

            _sut.Execute([item], Options(factor: 1, targetFolderPath: plainOut));
            _sut.Execute([item], Options(factor: 1, targetFolderPath: denoiseOut) with { DenoiseEnabled = true });

            using var plain = new MagickImage(Path.Combine(plainOut, "noise.png"));
            using var denoised = new MagickImage(Path.Combine(denoiseOut, "noise.png"));

            denoised.Width.Should().Be(plain.Width);
            denoised.Height.Should().Be(plain.Height);
            denoised.GetPixels().ToByteArray("RGB").Should().NotEqual(plain.GetPixels().ToByteArray("RGB"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static ResizeOptions Options(
        double factor = 0.5,
        ResizeOutputMode outputMode = ResizeOutputMode.TargetFolder,
        string? targetFolderPath = null) =>
        new(
            ResizeMode.ScaleFactor,
            ScaleFactor: factor,
            TargetWidth: 0,
            TargetHeight: 0,
            KeepAspectRatio: true,
            outputMode,
            TargetFolderPath: targetFolderPath ?? Path.Combine(Path.GetTempPath(), "FrameFileToolTests", "out"),
            ResamplerType.Bicubic);

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
}
