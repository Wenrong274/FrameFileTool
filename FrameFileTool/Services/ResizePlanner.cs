using System.Globalization;
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
    public IReadOnlyList<ResizePreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        ResizeOptions options,
        IReadOnlySet<string>? existingPaths = null)
    {
        // 優先驗證共用參數，有錯就讓全部項目標記錯誤
        var globalError = ValidateOptions(options);

        var targetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return files
            .Select((file, index) =>
            {
                if (globalError is not null)
                {
                    return MakeError(file, index + 1, globalError);
                }

                if (!string.IsNullOrWhiteSpace(file.DimensionReadError))
                {
                    return MakeError(file, index + 1, file.DimensionReadError);
                }

                var targetName = BuildTargetName(file.Name, options);
                var status = BuildStatus(options);
                var originalDimensions = file.Width > 0 ? $"{file.Width}×{file.Height}" : string.Empty;
                var targetDimensions = BuildTargetDimensions(file, options);
                var chooseTargetFolderOnExecute = ShouldChooseTargetFolderOnExecute(options);
                var targetPath = BuildTargetPath(file, options);
                var hasDuplicateTarget = options.OutputMode == ResizeOutputMode.TargetFolder &&
                    !chooseTargetFolderOnExecute &&
                    !targetPaths.Add(targetPath);
                var targetExists = TargetFileExists(targetPath, options, existingPaths);

                if (hasDuplicateTarget || targetExists)
                {
                    return new ResizePreviewItem
                    {
                        Index = index + 1,
                        FullPath = file.FullPath,
                        OriginalName = file.Name,
                        ActionKind = OperationActionKind.Error,
                        TargetName = targetName,
                        TargetPath = targetPath,
                        Status = hasDuplicateTarget ? "目標檔名重複" : "目標檔案已存在",
                        HasError = true,
                    };
                }

                return new ResizePreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    ActionKind = OperationActionKind.Resize,
                    TargetName = targetName,
                    TargetPath = targetPath,
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
        if (options.OutputMode == ResizeOutputMode.TargetFolder &&
            !ShouldChooseTargetFolderOnExecute(options) &&
            !PathSafetyValidator.IsSafeTargetDirectoryPath(options.TargetFolderPath))
        {
            return "目標資料夾路徑不安全";
        }

        return options.Mode switch
        {
            ResizeMode.ScaleFactor => ValidateScaleFactor(options.ScaleFactor),
            ResizeMode.Absolute => ValidateAbsolute(options),
            _ => null,
        };
    }

    private static string? ValidateScaleFactor(double factor)
    {
        if (double.IsNaN(factor) || double.IsInfinity(factor) || factor <= 0)
            return "縮放倍率必須大於 0";

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
        options.OutputMode == ResizeOutputMode.TargetFolder
            ? ShouldChooseTargetFolderOnExecute(options)
                ? "執行時選擇資料夾"
                : Path.Combine(options.TargetFolderPath, originalName)
            : originalName;

    private static string BuildTargetPath(FileItem file, ResizeOptions options) =>
        options.OutputMode == ResizeOutputMode.TargetFolder
            ? ShouldChooseTargetFolderOnExecute(options)
                ? string.Empty
                : Path.Combine(options.TargetFolderPath, file.Name)
            : file.FullPath;

    private static bool TargetFileExists(
        string targetPath,
        ResizeOptions options,
        IReadOnlySet<string>? existingPaths)
    {
        if (options.OutputMode != ResizeOutputMode.TargetFolder || ShouldChooseTargetFolderOnExecute(options))
        {
            return false;
        }

        return existingPaths?.Contains(targetPath) ?? false;
    }

    // ── Status 文字建立 ──────────────────────────────────────────

    private static string BuildStatus(ResizeOptions options)
    {
        var sizeDesc = options.Mode == ResizeMode.ScaleFactor
            ? $"{FormatScaleFactor(options.ScaleFactor)} 倍"
            : BuildAbsoluteSizeDesc(options);

        var resamplerDesc = ResamplerDescription(options.Resampler);
        var denoiseDesc = options.DenoiseMode switch
        {
            DenoiseMode.Detail => "，保留細節降噪",
            DenoiseMode.Standard => "，標準降噪",
            DenoiseMode.Strong => "，強力降噪",
            _ => string.Empty,
        };

        return $"縮放至 {sizeDesc}，{resamplerDesc}{denoiseDesc}";
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

        if (options.Mode == ResizeMode.ScaleFactor)
        {
            tw = Math.Max(1, (int)Math.Round(w * options.ScaleFactor));
            th = Math.Max(1, (int)Math.Round(h * options.ScaleFactor));
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

    private static string FormatScaleFactor(double factor) =>
        factor.ToString("0.####", CultureInfo.InvariantCulture);

    private static bool ShouldChooseTargetFolderOnExecute(ResizeOptions options) =>
        options.OutputMode == ResizeOutputMode.TargetFolder &&
        string.IsNullOrWhiteSpace(options.TargetFolderPath);

    // ── 輔助：建立錯誤項目 ───────────────────────────────────────

    private static ResizePreviewItem MakeError(FileItem file, int index, string message) =>
        new()
        {
            Index = index,
            FullPath = file.FullPath,
            OriginalName = file.Name,
            ActionKind = OperationActionKind.Error,
            TargetName = string.Empty,
            Status = message,
            HasError = true,
        };
}
