using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <summary>
/// 依據縮放設定，規劃批次縮放計畫。
/// Pure planner：不讀取圖片像素、不執行任何檔案 I/O、不修改 shared state。
/// </summary>
public sealed class ResizePlanner : IResizePlanner
{
    /// <inheritdoc/>
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        ResizeOptions options)
    {
        // 優先驗證共用參數，有錯就讓全部項目標記錯誤
        var globalError = ValidateOptions(options);

        return files
            .Select((file, index) =>
            {
                if (globalError is not null)
                {
                    return MakeError(file, index + 1, globalError);
                }

                var targetName = BuildTargetName(file.Name, options);
                var status = BuildStatus(options);
                var originalDimensions = file.Width > 0 ? $"{file.Width}×{file.Height}" : string.Empty;
                var targetDimensions = BuildTargetDimensions(file, options);

                return new OperationPreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    Action = OperationAction.Resize,
                    TargetName = targetName,
                    Status = status,
                    HasError = false,
                    OriginalDimensions = originalDimensions,
                    TargetDimensions = targetDimensions,
                };
            })
            .ToList();
    }

    // ── 驗證 ──────────────────────────────────────────────────────

    /// <summary>
    /// 驗證 options 是否合法。
    /// 回傳 null 代表合法；回傳字串代表錯誤訊息。
    /// </summary>
    private static string? ValidateOptions(ResizeOptions options)
    {
        // 子資料夾模式必須提供子資料夾名稱
        if (options.OutputMode == ResizeOutputMode.Subfolder &&
            string.IsNullOrWhiteSpace(options.SubfolderName))
        {
            return "子資料夾名稱不可為空";
        }

        return options.Mode switch
        {
            ResizeMode.Percentage => ValidatePercentage(options.ScalePercent),
            ResizeMode.Absolute => ValidateAbsolute(options),
            _ => null,
        };
    }

    private static string? ValidatePercentage(int percent)
    {
        if (percent <= 0)
            return "縮放比例必須大於 0%";

        if (percent > 10000)
            return "縮放比例不可超過 10000%";

        return null;
    }

    private static string? ValidateAbsolute(ResizeOptions options)
    {
        var hasWidth = options.TargetWidth > 0;
        var hasHeight = options.TargetHeight > 0;

        if (!hasWidth && !hasHeight)
            return "目標寬度與高度不可同時為零";

        // 不維持比例時，兩個方向都必須指定
        if (!options.KeepAspectRatio && (!hasWidth || !hasHeight))
            return "不維持比例時，目標寬度與高度都必須大於 0";

        return null;
    }

    // ── 目標路徑建立 ─────────────────────────────────────────────

    private static string BuildTargetName(string originalName, ResizeOptions options) =>
        options.OutputMode == ResizeOutputMode.Subfolder
            ? Path.Combine(options.SubfolderName, originalName)
            : originalName;

    // ── Status 文字建立 ──────────────────────────────────────────

    private static string BuildStatus(ResizeOptions options)
    {
        var sizeDesc = options.Mode == ResizeMode.Percentage
            ? $"{options.ScalePercent}%"
            : BuildAbsoluteSizeDesc(options);

        var resamplerDesc = ResamplerDescription(options.Resampler);

        return $"縮放至 {sizeDesc}，{resamplerDesc}";
    }

    private static string BuildAbsoluteSizeDesc(ResizeOptions options)
    {
        var hasWidth = options.TargetWidth > 0;
        var hasHeight = options.TargetHeight > 0;

        return (hasWidth, hasHeight) switch
        {
            (true, true) => $"{options.TargetWidth}×{options.TargetHeight}",
            (true, false) => $"寬 {options.TargetWidth}px（等比）",
            (false, true) => $"高 {options.TargetHeight}px（等比）",
            _ => "未指定尺寸",
        };
    }

    // ── 目標尺寸計算 ─────────────────────────────────────────────

    /// <summary>
    /// 依縮放設定與來源圖片尺寸計算預期輸出尺寸字串。
    /// 若來源尺寸未知（Width/Height == 0）則回傳空字串。
    /// </summary>
    private static string BuildTargetDimensions(FileItem file, ResizeOptions options)
    {
        var w = file.Width;
        var h = file.Height;

        if (w <= 0 || h <= 0)
            return string.Empty;

        int tw, th;

        if (options.Mode == ResizeMode.Percentage)
        {
            tw = Math.Max(1, (int)Math.Round(w * options.ScalePercent / 100.0));
            th = Math.Max(1, (int)Math.Round(h * options.ScalePercent / 100.0));
        }
        else
        {
            var hasW = options.TargetWidth > 0;
            var hasH = options.TargetHeight > 0;

            if (!options.KeepAspectRatio)
            {
                tw = options.TargetWidth;
                th = options.TargetHeight;
            }
            else if (hasW && !hasH)
            {
                tw = options.TargetWidth;
                th = Math.Max(1, (int)Math.Round(h * (double)options.TargetWidth / w));
            }
            else if (!hasW && hasH)
            {
                th = options.TargetHeight;
                tw = Math.Max(1, (int)Math.Round(w * (double)options.TargetHeight / h));
            }
            else
            {
                // 兩邊都有指定，維持比例置入框內（fit within box）
                var scale = Math.Min((double)options.TargetWidth / w, (double)options.TargetHeight / h);
                tw = Math.Max(1, (int)Math.Round(w * scale));
                th = Math.Max(1, (int)Math.Round(h * scale));
            }
        }

        return $"{tw}×{th}";
    }

    private static string ResamplerDescription(ResamplerType resampler) => resampler switch
    {
        ResamplerType.Lanczos3 => "高品質縮小（Lanczos3）",
        ResamplerType.CatmullRom => "高品質放大（CatmullRom）",
        ResamplerType.NearestNeighbor => "像素精準（NearestNeighbor）",
        ResamplerType.MitchellNetravali => "銳利優先（MitchellNetravali）",
        _ => "一般用途（Bicubic）",
    };

    // ── 輔助：建立錯誤項目 ───────────────────────────────────────

    private static OperationPreviewItem MakeError(FileItem file, int index, string message) =>
        new()
        {
            Index = index,
            FullPath = file.FullPath,
            OriginalName = file.Name,
            Action = OperationAction.Error,
            TargetName = string.Empty,
            Status = message,
            HasError = true,
        };
}
