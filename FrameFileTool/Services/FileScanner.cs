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
    public FileScanResult Scan(
        string folder,
        IEnumerable<string> extensions,
        bool includeSubfolders)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return new FileScanResult([], []);
        }

        // 建立副檔名 HashSet，允許大小寫不敏感比對（.PNG = .png）
        var extensionSet = extensions
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(NormalizeExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (extensionSet.Count == 0)
        {
            return new FileScanResult([], []);
        }

        var errors = new List<string>();

        var files = EnumerateFilesSafely(folder, includeSubfolders, errors)
            .Select(path => new FileInfo(path))
            .Where(info => extensionSet.Contains(info.Extension))
            .Select(info => CreateFileItem(info, folder, errors))
            .Where(item => item is not null)
            .Cast<FileItem>()
            // 先依資料夾排序，讓子資料夾各自成群
            .OrderBy(item => item.DirectoryPath, StringComparer.OrdinalIgnoreCase)
            // 同一資料夾內以自然排序處理序列編號
            .ThenBy(item => item.Name, _naturalStringComparer)
            .ToList();

        return new FileScanResult(files, errors);
    }

    /// <summary>確保副檔名帶有前置點，例如將 "png" 轉為 ".png"。</summary>
    private static string NormalizeExtension(string extension) =>
        extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;

    private static IEnumerable<string> EnumerateFilesSafely(
        string folder,
        bool includeSubfolders,
        List<string> errors)
    {
        foreach (var file in EnumerateDirectoryFiles(folder, errors))
        {
            yield return file;
        }

        if (!includeSubfolders)
        {
            yield break;
        }

        foreach (var child in EnumerateChildDirectories(folder, errors))
        {
            foreach (var file in EnumerateFilesSafely(child, includeSubfolders: true, errors))
            {
                yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateDirectoryFiles(string folder, List<string> errors)
    {
        IReadOnlyList<string> files;

        try
        {
            files = Directory
                .EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .ToList();
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            errors.Add($"{folder}: {ex.Message}");
            yield break;
        }

        foreach (var file in files)
        {
            yield return file;
        }
    }

    private static IEnumerable<string> EnumerateChildDirectories(string folder, List<string> errors)
    {
        IReadOnlyList<string> directories;

        try
        {
            directories = Directory
                .EnumerateDirectories(folder, "*", SearchOption.TopDirectoryOnly)
                .ToList();
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            errors.Add($"{folder}: {ex.Message}");
            yield break;
        }

        foreach (var directory in directories)
        {
            yield return directory;
        }
    }

    private static FileItem? CreateFileItem(FileInfo info, string fallbackFolder, List<string> errors)
    {
        try
        {
            return new FileItem(
                info.FullName,
                info.DirectoryName ?? fallbackFolder,
                info.Name,
                info.Extension,
                info.Length);
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            errors.Add($"{info.FullName}: {ex.Message}");
            return null;
        }
    }

    private static bool IsRecoverableFileSystemException(Exception ex) =>
        ex is UnauthorizedAccessException or IOException or System.Security.SecurityException;
}
