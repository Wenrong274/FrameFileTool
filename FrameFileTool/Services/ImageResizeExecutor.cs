using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using ImageMagick;

namespace FrameFileTool.Services;

/// <summary>
/// 執行實際的圖片縮放副作用，使用 Magick.NET（Apache 2.0）進行高品質重取樣。
/// 只執行 <see cref="OperationAction.Resize"/> 且 <c>HasError = false</c> 的項目。
///
/// 覆寫模式安全機制：
///   先寫出暫存檔 → 成功後以 File.Move 覆蓋原始 → 失敗時保留原始檔案不受影響。
/// </summary>
public sealed class ImageResizeExecutor : IImageResizeExecutor
{
    /// <inheritdoc/>
    public OperationResult Execute(
        IEnumerable<OperationPreviewItem> previewItems,
        ResizeOptions options)
    {
        var result = new OperationResult();

        var targets = previewItems
            .Where(item => item.Action == OperationAction.Resize && !item.HasError)
            .ToList();

        foreach (var item in targets)
        {
            try
            {
                ProcessItem(item, options, result);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{item.OriginalName}: 未預期的錯誤，{ex.Message}");
            }
        }

        return result;
    }

    // ── 單一項目處理 ─────────────────────────────────────────────

    private static void ProcessItem(
        OperationPreviewItem item,
        ResizeOptions options,
        OperationResult result)
    {
        if (!File.Exists(item.FullPath))
        {
            result.Errors.Add($"{item.OriginalName}: 來源檔案不存在");
            return;
        }

        var outputPath = ResolveOutputPath(item, options);
        EnsureDirectoryExists(outputPath);

        if (options.OutputMode == ResizeOutputMode.Overwrite)
        {
            WriteWithTempFile(item, options, outputPath, result);
        }
        else
        {
            WriteDirectly(item, options, outputPath, result);
        }
    }

    // ── 覆寫模式：暫存中轉確保安全 ─────────────────────────────

    private static void WriteWithTempFile(
        OperationPreviewItem item,
        ResizeOptions options,
        string outputPath,
        OperationResult result)
    {
        var directory = Path.GetDirectoryName(item.FullPath) ?? string.Empty;
        var tempPath = Path.Combine(directory, $".__FrameFileTool_resize_{Guid.NewGuid():N}.tmp");

        try
        {
            ResizeAndSave(item.FullPath, tempPath, options);
            File.Move(tempPath, outputPath, overwrite: true);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            // 暫存檔若存在則清理，原始檔保持不變
            TryDeleteTemp(tempPath);
            result.Errors.Add($"{item.OriginalName}: 縮放失敗，{ex.Message}");
        }
    }

    // ── 子資料夾模式：直接寫出 ───────────────────────────────────

    private static void WriteDirectly(
        OperationPreviewItem item,
        ResizeOptions options,
        string outputPath,
        OperationResult result)
    {
        try
        {
            ResizeAndSave(item.FullPath, outputPath, options);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"{item.OriginalName}: 縮放失敗，{ex.Message}");
        }
    }

    // ── 核心縮放邏輯（Magick.NET）────────────────────────────────

    private static void ResizeAndSave(string sourcePath, string targetPath, ResizeOptions options)
    {
        using var image = new MagickImage(sourcePath);

        var geometry = BuildGeometry(image, options);
        image.FilterType = ToFilterType(options.Resampler);
        image.Resize(geometry);

        image.Write(targetPath);
    }

    /// <summary>依 ResizeOptions 計算目標 MagickGeometry。</summary>
    private static MagickGeometry BuildGeometry(MagickImage image, ResizeOptions options)
    {
        if (options.Mode == ResizeMode.Percentage)
        {
            var pct = (uint)options.ScalePercent;
            return new MagickGeometry($"{pct}%");
        }

        // 絕對模式
        var width = (uint)options.TargetWidth;
        var height = (uint)options.TargetHeight;

        if (options.KeepAspectRatio)
        {
            // 單邊指定：讓 ImageMagick 依比例自動計算另一邊
            return (width, height) switch
            {
                ( > 0, 0) => new MagickGeometry(width, 0),
                (0, > 0) => new MagickGeometry(0, height),
                _ => new MagickGeometry(width, height) { IgnoreAspectRatio = false },
            };
        }

        // 不維持比例：強制指定兩邊
        return new MagickGeometry(width, height) { IgnoreAspectRatio = true };
    }

    // ── ResamplerType → FilterType 對應 ──────────────────────────

    private static FilterType ToFilterType(ResamplerType resampler) => resampler switch
    {
        ResamplerType.Lanczos3 => FilterType.Lanczos,
        ResamplerType.CatmullRom => FilterType.Catrom,
        ResamplerType.NearestNeighbor => FilterType.Point,
        ResamplerType.MitchellNetravali => FilterType.Mitchell,
        _ => FilterType.Cubic,
    };

    // ── 路徑輔助 ─────────────────────────────────────────────────

    private static string ResolveOutputPath(OperationPreviewItem item, ResizeOptions options)
    {
        var directory = Path.GetDirectoryName(item.FullPath) ?? string.Empty;

        return options.OutputMode == ResizeOutputMode.Subfolder
            ? Path.Combine(directory, options.SubfolderName, item.OriginalName)
            : item.FullPath;
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    private static void TryDeleteTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
        catch
        {
            // 清理失敗不影響主流程，暫存檔名有 .__FrameFileTool_ 前綴，不會與正式檔案混淆
        }
    }
}
