using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <summary>
/// 依據降噪設定，規劃批次降噪計畫。
/// Pure planner：不讀取圖片像素、不執行任何檔案 I/O、不修改 shared state。
/// 批次降噪固定覆寫原檔，因此不需要目標路徑衝突偵測。
/// </summary>
public sealed class DenoisePlanner : IDenoisePlanner
{
    /// <inheritdoc/>
    public IReadOnlyList<OperationPreviewItem> Plan(
        IReadOnlyList<FileItem> files,
        DenoiseOptions options)
    {
        // UI 的模式選單不提供 Off，此驗證為 planner 的最後防線
        var globalError = options.Mode == DenoiseMode.Off
            ? "未選擇降噪模式"
            : null;

        var status = BuildStatus(options.Mode);

        return files
            .Select((file, index) => globalError is not null
                ? MakeError(file, index + 1, globalError)
                : new OperationPreviewItem
                {
                    Index = index + 1,
                    FullPath = file.FullPath,
                    OriginalName = file.Name,
                    ActionKind = OperationActionKind.Denoise,
                    TargetName = file.Name,
                    TargetPath = file.FullPath,
                    Status = status,
                    HasError = false,
                })
            .ToList();
    }

    // ── Status 文字建立 ──────────────────────────────────────────

    private static string BuildStatus(DenoiseMode mode) => mode switch
    {
        DenoiseMode.Detail => "保留細節降噪（覆寫原檔）",
        DenoiseMode.Standard => "標準降噪（覆寫原檔）",
        DenoiseMode.Strong => "強力降噪（覆寫原檔）",
        _ => string.Empty,
    };

    // ── 輔助：建立錯誤項目 ───────────────────────────────────────

    private static OperationPreviewItem MakeError(FileItem file, int index, string message) =>
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
