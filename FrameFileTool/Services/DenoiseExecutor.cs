using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using ImageMagick;

namespace FrameFileTool.Services;

/// <summary>
/// 執行實際的圖片降噪副作用，使用 Magick.NET 套用降噪後覆寫原檔。
/// 只執行 <see cref="OperationActionKind.Denoise"/> 且 <c>HasError = false</c> 的項目。
///
/// 覆寫安全機制（與 ImageResizeExecutor 的覆寫模式相同）：
///   先寫出暫存檔 → 成功後以 File.Move 覆蓋原始 → 失敗時保留原始檔案不受影響。
/// </summary>
public sealed class DenoiseExecutor : IDenoiseExecutor
{
    /// <inheritdoc/>
    public OperationResult Execute(
        IEnumerable<OperationPreviewItem> previewItems,
        DenoiseOptions options)
    {
        var result = new OperationResult();
        var targets = FilterTargets(previewItems, result);

        if (targets.Count == 0 || !ValidateOptions(options, targets, result))
        {
            return result;
        }

        foreach (var item in targets)
        {
            ProcessItemSafely(item, options, result);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ExecuteAsync(
        IEnumerable<OperationPreviewItem> previewItems,
        DenoiseOptions options,
        IProgress<ResizeProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new OperationResult();
        var targets = FilterTargets(previewItems, result);

        if (targets.Count == 0 || !ValidateOptions(options, targets, result))
        {
            return result;
        }

        // 在背景執行緒上跑 CPU/IO 密集的降噪迴圈，Progress<T> 會自動 marshal 回 UI 執行緒
        await Task.Run(() =>
        {
            for (var i = 0; i < targets.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Canceled = true;
                    result.SkippedCount += targets.Count - i;
                    break;
                }

                var item = targets[i];
                progress?.Report(new ResizeProgressReport(i + 1, targets.Count, item.OriginalName));
                ProcessItemSafely(item, options, result);
            }
        });

        return result;
    }

    // ── 目標過濾與驗證 ──────────────────────────────────────────

    private static List<OperationPreviewItem> FilterTargets(
        IEnumerable<OperationPreviewItem> previewItems,
        OperationResult result)
    {
        var items = previewItems.ToList();
        var targets = items
            .Where(item => item.IsIncluded && item.ActionKind == OperationActionKind.Denoise && !item.HasError)
            .ToList();
        result.SkippedCount = items.Count - targets.Count;
        return targets;
    }

    private static bool ValidateOptions(
        DenoiseOptions options,
        IReadOnlyCollection<OperationPreviewItem> targets,
        OperationResult result)
    {
        if (options.Mode != DenoiseMode.Off)
        {
            return true;
        }

        result.SkippedCount += targets.Count;
        result.Errors.Add("未選擇降噪模式");
        return false;
    }

    // ── 單一項目處理（暫存中轉確保安全） ─────────────────────────

    private static void ProcessItemSafely(
        OperationPreviewItem item,
        DenoiseOptions options,
        OperationResult result)
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

    private static void ProcessItem(
        OperationPreviewItem item,
        DenoiseOptions options,
        OperationResult result)
    {
        if (!File.Exists(item.FullPath))
        {
            result.Errors.Add($"{item.OriginalName}: 來源檔案不存在");
            return;
        }

        var directory = Path.GetDirectoryName(item.FullPath) ?? string.Empty;
        var tempPath = Path.Combine(directory, $".__FrameFileTool_denoise_{Guid.NewGuid():N}.tmp");

        try
        {
            DenoiseAndSave(item.FullPath, tempPath, options.Mode);
            File.Move(tempPath, item.FullPath, overwrite: true);
            result.SuccessCount++;
        }
        catch (Exception ex)
        {
            // 暫存檔若存在則清理，原始檔保持不變
            TryDeleteTemp(tempPath);
            result.Errors.Add($"{item.OriginalName}: 降噪失敗，{ex.Message}");
        }
    }

    private static void DenoiseAndSave(string sourcePath, string targetPath, DenoiseMode mode)
    {
        using var image = new MagickImage(sourcePath);
        DenoiseImageProcessor.ApplyDenoise(image, mode);

        // 暫存檔以 .tmp 結尾，必須依原始副檔名明確指定輸出格式
        image.Write(targetPath, image.Format);
    }

    private static void TryDeleteTemp(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (IOException)
        {
            // 清理失敗不影響主要錯誤回報
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
