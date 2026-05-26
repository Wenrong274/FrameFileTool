using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace FrameFileTool.Services;

/// <summary>
/// 執行實際的檔案系統副作用：移到回收桶、批次改名。
/// 操作前必須先由 planner 產出預覽計畫，executor 只執行標記為目標動作
/// 且 <see cref="OperationPreviewItem.HasError"/> 為 false 的項目。
/// </summary>
public sealed class FileOperationExecutor : IFileOperationExecutor
{
    /// <inheritdoc/>
    public OperationResult DeleteToRecycleBin(IEnumerable<OperationPreviewItem> previewItems)
    {
        var result = new OperationResult();
        var items = previewItems.ToList();

        // 只處理標記為刪除且無錯誤的項目
        var targets = items
            .Where(item => item.Action == OperationAction.Delete && !item.HasError)
            .ToList();
        result.SkippedCount = items.Count - targets.Count;

        foreach (var item in targets)
        {
            try
            {
                if (!File.Exists(item.FullPath))
                {
                    result.Errors.Add($"{item.OriginalName}: 檔案不存在");
                    continue;
                }

                // 使用 VisualBasic FileSystem 將檔案移到回收桶，而非永久刪除
                FileSystem.DeleteFile(
                    item.FullPath,
                    UIOption.OnlyErrorDialogs,
                    RecycleOption.SendToRecycleBin);

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{item.OriginalName}: {ex.Message}");
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public OperationResult RenameFiles(IEnumerable<OperationPreviewItem> previewItems)
    {
        var result = new OperationResult();
        var items = previewItems.ToList();

        // 只處理標記為改名且無錯誤的項目
        var targets = items
            .Where(item => item.Action == OperationAction.Rename && !item.HasError)
            .ToList();
        result.SkippedCount = items.Count - targets.Count;

        // 暫存改名清單：先全部移到暫存檔名，再一次移到最終檔名。
        // 這樣可避免 A→B、B→C 這類改名鏈造成的撞名問題。
        var tempItems = new List<(string OriginalPath, string TempPath, string FinalPath, string OriginalName)>(
            targets.Count);

        // 第一階段：來源 → 暫存檔名
        foreach (var item in targets)
        {
            try
            {
                if (!File.Exists(item.FullPath))
                {
                    result.Errors.Add($"{item.OriginalName}: 檔案不存在");
                    continue;
                }

                var directory = Path.GetDirectoryName(item.FullPath) ?? string.Empty;

                if (!PathSafetyValidator.IsSafeFileName(item.TargetName))
                {
                    result.Errors.Add($"{item.OriginalName}: 目標檔名不安全");
                    continue;
                }

                var finalPath = Path.Combine(directory, item.TargetName);

                // 使用 GUID 確保暫存檔名唯一
                var tempPath = Path.Combine(directory, $".__FrameFileTool_{Guid.NewGuid():N}.tmp");

                File.Move(item.FullPath, tempPath);
                tempItems.Add((item.FullPath, tempPath, finalPath, item.OriginalName));
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{item.OriginalName}: 暫存改名失敗，{ex.Message}");
            }
        }

        // 第二階段：暫存檔名 → 最終目標檔名
        foreach (var (originalPath, tempPath, finalPath, originalName) in tempItems)
        {
            try
            {
                File.Move(tempPath, finalPath);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                var restoreMessage = TryRestoreOriginalPath(tempPath, originalPath);
                result.Errors.Add($"{originalName}: 最終改名失敗，{ex.Message}{restoreMessage}");
            }
        }

        return result;
    }

    /// <summary>最終改名失敗時，嘗試把暫存檔移回原始路徑。</summary>
    private static string TryRestoreOriginalPath(string tempPath, string originalPath)
    {
        if (!File.Exists(tempPath))
        {
            return "。暫存檔已不存在，無法自動還原。";
        }

        try
        {
            if (File.Exists(originalPath))
            {
                return $"。原始路徑已存在，暫存檔保留於 {tempPath}。";
            }

            File.Move(tempPath, originalPath);
            return "。已還原原始檔名。";
        }
        catch (Exception ex)
        {
            return $"。還原失敗，暫存檔保留於 {tempPath}，原因：{ex.Message}";
        }
    }
}
