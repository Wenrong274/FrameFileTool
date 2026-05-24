using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;
using Microsoft.VisualBasic.FileIO;

namespace FrameFileTool.Services;

public sealed class FileOperationExecutor : IFileOperationExecutor
{
    public OperationResult DeleteToRecycleBin(IEnumerable<OperationPreviewItem> previewItems)
    {
        var result = new OperationResult();
        var targets = previewItems.Where(item => item.Action == "刪除" && !item.HasError).ToList();

        foreach (var item in targets)
        {
            try
            {
                if (!File.Exists(item.FullPath))
                {
                    result.Errors.Add($"{item.OriginalName}: 檔案不存在");
                    continue;
                }

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

    public OperationResult RenameFiles(IEnumerable<OperationPreviewItem> previewItems)
    {
        var result = new OperationResult();
        var targets = previewItems
            .Where(item => item.Action == "改名" && !item.HasError)
            .ToList();

        var tempItems = new List<(string TempPath, string FinalPath, string OriginalName)>();

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
                var finalPath = Path.Combine(directory, item.TargetName);
                var tempPath = Path.Combine(directory, $".__FrameFileTool_{Guid.NewGuid():N}.tmp");

                File.Move(item.FullPath, tempPath);
                tempItems.Add((tempPath, finalPath, item.OriginalName));
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{item.OriginalName}: 暫存改名失敗，{ex.Message}");
            }
        }

        foreach (var item in tempItems)
        {
            try
            {
                File.Move(item.TempPath, item.FinalPath);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{item.OriginalName}: 最終改名失敗，{ex.Message}");
            }
        }

        return result;
    }
}

