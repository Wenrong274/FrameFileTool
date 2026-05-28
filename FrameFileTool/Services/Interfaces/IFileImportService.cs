using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 將使用者拖放的檔案或資料夾路徑轉成可加入掃描清單的圖片檔案。
/// </summary>
public interface IFileImportService
{
    /// <summary>
    /// 匯入拖放路徑，並依目前副檔名與子資料夾設定過濾檔案。
    /// </summary>
    public FileImportResult Import(
        IReadOnlyList<string> paths,
        IEnumerable<string> extensions,
        bool includeSubfolders,
        IReadOnlySet<string> existingPaths);
}
