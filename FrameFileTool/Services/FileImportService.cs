using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <inheritdoc/>
public sealed class FileImportService(IFileScanner scanner) : IFileImportService
{
    private readonly NaturalStringComparer _naturalStringComparer = new();

    /// <inheritdoc/>
    public FileImportResult Import(
        IReadOnlyList<string> paths,
        IEnumerable<string> extensions,
        bool includeSubfolders,
        IReadOnlySet<string> existingPaths)
    {
        var extensionSet = extensions
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Select(NormalizeExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (extensionSet.Count == 0)
        {
            return new FileImportResult([], ["未選擇任何副檔名，無法匯入拖放資料"]);
        }

        var files = new List<FileItem>();
        var errors = new List<string>();
        var knownPaths = new HashSet<string>(existingPaths, StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            ImportPath(path, extensionSet, includeSubfolders, knownPaths, files, errors);
        }

        var sortedFiles = files
            .OrderBy(file => file.DirectoryPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file.Name, _naturalStringComparer)
            .ToList();

        return new FileImportResult(sortedFiles, errors);
    }

    private void ImportPath(
        string path,
        IReadOnlySet<string> extensionSet,
        bool includeSubfolders,
        HashSet<string> knownPaths,
        List<FileItem> files,
        List<string> errors)
    {
        if (File.Exists(path))
        {
            ImportFile(path, extensionSet, knownPaths, files, errors);
            return;
        }

        if (Directory.Exists(path))
        {
            ImportDirectory(path, extensionSet, includeSubfolders, knownPaths, files, errors);
            return;
        }

        errors.Add($"{path}: 路徑不存在");
    }

    private void ImportDirectory(
        string folder,
        IReadOnlySet<string> extensionSet,
        bool includeSubfolders,
        HashSet<string> knownPaths,
        List<FileItem> files,
        List<string> errors)
    {
        var scanResult = scanner.Scan(folder, extensionSet, includeSubfolders);

        foreach (var file in scanResult.Files)
        {
            AddFileIfNew(file, knownPaths, files, errors);
        }

        foreach (var error in scanResult.Errors)
        {
            errors.Add(error);
        }
    }

    private static void ImportFile(
        string path,
        IReadOnlySet<string> extensionSet,
        HashSet<string> knownPaths,
        List<FileItem> files,
        List<string> errors)
    {
        try
        {
            var info = new FileInfo(path);
            if (!extensionSet.Contains(info.Extension))
            {
                errors.Add($"{info.Name}: 副檔名不支援");
                return;
            }

            var file = new FileItem(
                info.FullName,
                info.DirectoryName ?? string.Empty,
                info.Name,
                info.Extension,
                info.Length);

            AddFileIfNew(file, knownPaths, files, errors);
        }
        catch (Exception ex) when (IsRecoverableFileSystemException(ex))
        {
            errors.Add($"{path}: {ex.Message}");
        }
    }

    private static void AddFileIfNew(
        FileItem file,
        HashSet<string> knownPaths,
        List<FileItem> files,
        List<string> errors)
    {
        if (!knownPaths.Add(file.FullPath))
        {
            errors.Add($"{file.Name}: 已存在於清單，略過");
            return;
        }

        files.Add(file);
    }

    private static string NormalizeExtension(string extension) =>
        extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;

    private static bool IsRecoverableFileSystemException(Exception ex) =>
        ex is UnauthorizedAccessException or IOException or System.Security.SecurityException
            or ArgumentException or NotSupportedException;
}
