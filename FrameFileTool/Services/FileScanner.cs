using FrameFileTool.Models;

namespace FrameFileTool.Services;

public sealed class FileScanner
{
    private readonly NaturalStringComparer _naturalStringComparer = new();

    public IReadOnlyList<FileItem> Scan(string folder, IEnumerable<string> extensions, bool includeSubfolders)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return Array.Empty<FileItem>();
        }

        var extensionSet = extensions
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Select(NormalizeExtension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (extensionSet.Count == 0)
        {
            return Array.Empty<FileItem>();
        }

        var option = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        return Directory.EnumerateFiles(folder, "*.*", option)
            .Select(path => new FileInfo(path))
            .Where(info => extensionSet.Contains(info.Extension))
            .Select(info => new FileItem(
                info.FullName,
                info.DirectoryName ?? folder,
                info.Name,
                info.Extension,
                info.Length))
            .OrderBy(item => item.DirectoryPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, _naturalStringComparer)
            .ToList();
    }

    private static string NormalizeExtension(string extension)
    {
        return extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : "." + extension;
    }
}
