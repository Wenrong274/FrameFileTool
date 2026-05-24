using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <summary>
/// 掃描指定資料夾內符合副檔名的圖片檔，
/// 並依「資料夾路徑 → 自然排序檔名」排序後回傳。
/// </summary>
public sealed class FileScanner : IFileScanner
{
    private readonly NaturalStringComparer _naturalStringComparer = new();

    /// <inheritdoc/>
    public IReadOnlyList<FileItem> Scan(
        string folder,
        IEnumerable<string> extensions,
        bool includeSubfolders)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return [];
        }

        // 建立副檔名 HashSet，允許大小寫不敏感比對（.PNG = .png）
        var extensionSet = extensions
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(NormalizeExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (extensionSet.Count == 0)
        {
            return [];
        }

        var option = includeSubfolders
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        return Directory.EnumerateFiles(folder, "*.*", option)
            .Select(path => new FileInfo(path))
            .Where(info => extensionSet.Contains(info.Extension))
            .Select(info => new FileItem(
                info.FullName,
                info.DirectoryName ?? folder,
                info.Name,
                info.Extension,
                info.Length))
            // 先依資料夾排序，讓子資料夾各自成群
            .OrderBy(item => item.DirectoryPath, StringComparer.OrdinalIgnoreCase)
            // 同一資料夾內以自然排序處理序列編號
            .ThenBy(item => item.Name, _naturalStringComparer)
            .ToList();
    }

    /// <summary>確保副檔名帶有前置點，例如將 "png" 轉為 ".png"。</summary>
    private static string NormalizeExtension(string extension) =>
        extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;
}
