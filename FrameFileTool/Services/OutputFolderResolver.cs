using System.Globalization;
using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <inheritdoc/>
public sealed class OutputFolderResolver : IOutputFolderResolver
{
    /// <inheritdoc/>
    public ResolvedOutputFolder ResolveForResize(
        string sourceFolderPath,
        string selectedTargetFolderPath,
        ResizeOptions options)
    {
        // 拖放匯入的檔案沒有來源資料夾，此時不可能與目標相同，也不能拿空字串去正規化路徑。
        if (string.IsNullOrWhiteSpace(sourceFolderPath) || string.IsNullOrWhiteSpace(selectedTargetFolderPath))
        {
            return new(selectedTargetFolderPath, WasAutoRedirected: false, LogMessage: null);
        }

        var normalizedSource = NormalizeFolderPath(sourceFolderPath);
        var normalizedTarget = NormalizeFolderPath(selectedTargetFolderPath);

        if (!string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return new(selectedTargetFolderPath, WasAutoRedirected: false, LogMessage: null);
        }

        var sourceFolderName = BuildSafeFolderName(Path.GetFileName(normalizedSource));
        var suffix = BuildResizeSuffix(options);
        var targetFolder = Path.Combine(normalizedSource, $"{sourceFolderName}{suffix}");

        return new(
            targetFolder,
            WasAutoRedirected: true,
            LogMessage: $"輸出資料夾與來源相同，原選擇：{selectedTargetFolderPath}，實際輸出：{targetFolder}");
    }

    private static string BuildSafeFolderName(string folderName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = folderName
            .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
            .ToArray();

        var safeName = new string(chars).Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(safeName) ? "output" : safeName;
    }

    private static string BuildResizeSuffix(ResizeOptions options) =>
        options.Mode == ResizeMode.ScaleFactor
            ? $"_x{options.ScaleFactor.ToString("0.####", CultureInfo.InvariantCulture)}"
            : $"_{options.TargetWidth}x{options.TargetHeight}";

    private static string NormalizeFolderPath(string path)
    {
        var trimmedPath = Path.TrimEndingDirectorySeparator(path.Trim());
        var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmedPath));

        var trailingDots = CountTrailingDotsForActualFolderName(Path.GetFileName(trimmedPath));
        if (trailingDots == 0 || normalizedPath.EndsWith(new string('.', trailingDots), StringComparison.Ordinal))
        {
            return normalizedPath;
        }

        return normalizedPath + new string('.', trailingDots);
    }

    private static int CountTrailingDotsForActualFolderName(string pathSegment) =>
        pathSegment is "." or ".."
            ? 0
            : CountTrailingDots(pathSegment);

    private static int CountTrailingDots(string pathSegment)
    {
        var count = 0;
        for (var index = pathSegment.Length - 1; index >= 0 && pathSegment[index] == '.'; index--)
        {
            count++;
        }

        return count;
    }
}
