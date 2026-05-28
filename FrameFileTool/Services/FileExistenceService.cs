using System.IO;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <summary>
/// 集中處理檔案存在性查詢，避免 planner 直接讀取檔案系統。
/// </summary>
public sealed class FileExistenceService : IFileExistenceService
{
    public IReadOnlySet<string> GetExistingPaths(IEnumerable<string> paths)
    {
        var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(path))
                {
                    existingPaths.Add(path);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // 無效路徑會在 planner 的輸入驗證中回報；存在性查詢只回傳可確認存在的路徑。
            }
        }

        return existingPaths;
    }
}
