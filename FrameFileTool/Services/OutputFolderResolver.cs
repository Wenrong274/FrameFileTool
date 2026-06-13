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
        var normalizedSource = NormalizeFolderPath(sourceFolderPath);
        var normalizedTarget = NormalizeFolderPath(selectedTargetFolderPath);

        if (!string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
        {
            return new(selectedTargetFolderPath, WasAutoRedirected: false, LogMessage: null);
        }

        var sourceFolderName = Path.GetFileName(normalizedSource);
        var suffix = options.Mode == ResizeMode.ScaleFactor
            ? $"_x{options.ScaleFactor.ToString("0.####", CultureInfo.InvariantCulture)}"
            : $"_{options.TargetWidth}x{options.TargetHeight}";
        var targetFolder = Path.Combine(normalizedSource, $"{sourceFolderName}{suffix}");

        return new(
            targetFolder,
            WasAutoRedirected: true,
            LogMessage: $"輸出資料夾與來源相同，已自動改用：{targetFolder}");
    }

    private static string NormalizeFolderPath(string path) =>
        Path.GetFullPath(path.Trim())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
