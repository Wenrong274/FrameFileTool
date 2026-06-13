using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using ImageMagick;

namespace FrameFileTool.Services;

/// <summary>
/// 產生降噪局部預覽圖。對來源圖片進行中央裁切（最大 256×256），
/// 依各 <see cref="DenoiseMode"/> 套用降噪後，輸出 PNG 位元組陣列。
/// 使用 <see cref="Task.Run"/> 在背景執行緒上執行，不阻塞 UI 執行緒。
/// </summary>
public sealed class DenoisePreviewService : IDenoisePreviewService
{
    private const int _maxPreviewSize = 256;

    /// <inheritdoc/>
    public Task<DenoisePreviewResult> GeneratePreviewAsync(
        string sourceImagePath,
        IReadOnlyList<DenoiseMode> modes,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Generate(sourceImagePath, modes), cancellationToken);
    }

    private static DenoisePreviewResult Generate(
        string sourceImagePath,
        IReadOnlyList<DenoiseMode> modes)
    {
        try
        {
            using var source = new MagickImage(sourceImagePath);
            var crop = BuildCropGeometry(source);
            var previews = new Dictionary<DenoiseMode, byte[]>(modes.Count);

            foreach (var mode in modes)
            {
                using var preview = new MagickImage(source);
                preview.Crop(crop);
                DenoiseImageProcessor.ApplyDenoise(preview, mode);
                var bytes = preview.ToByteArray(MagickFormat.Png);
                if (bytes is not null)
                    previews[mode] = bytes;
            }

            return new DenoisePreviewResult(previews);
        }
        catch (Exception ex)
        {
            return new DenoisePreviewResult(
                new Dictionary<DenoiseMode, byte[]>(),
                $"無法產生預覽：{ex.Message}");
        }
    }

    private static MagickGeometry BuildCropGeometry(MagickImage source)
    {
        var size = Math.Min(_maxPreviewSize, (int)Math.Min(source.Width, source.Height));
        var x = (int)((source.Width - size) / 2);
        var y = (int)((source.Height - size) / 2);
        return new MagickGeometry(x, y, (uint)size, (uint)size);
    }
}
