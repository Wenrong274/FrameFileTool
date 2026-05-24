using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 掃描指定資料夾內符合副檔名的圖片檔，並以自然排序回傳結果。
/// </summary>
public interface IFileScanner
{
    IReadOnlyList<FileItem> Scan(string folder, IEnumerable<string> extensions, bool includeSubfolders);
}
