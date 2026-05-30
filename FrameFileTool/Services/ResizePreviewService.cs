using System.IO;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

/// <inheritdoc/>
public sealed class ResizePreviewService(
    IImageDimensionReader dimensionReader,
    IFileExistenceService fileExistenceService,
    IResizePlanner resizePlanner) : IResizePreviewService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ResizePreviewItem>> BuildPreviewAsync(
        IReadOnlyList<FileItem> files,
        ResizeOptions options,
        CancellationToken cancellationToken = default)
    {
        var enrichedFiles = await Task.Run(
            () => files.Select(EnrichDimensions).ToList(),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var existingPaths = GetExistingResizeTargetPaths(enrichedFiles, options);
        return resizePlanner.Plan(enrichedFiles, options, existingPaths);
    }

    private FileItem EnrichDimensions(FileItem file)
    {
        try
        {
            var (width, height) = dimensionReader.Read(file.FullPath);
            return file with { Width = width, Height = height };
        }
        catch
        {
            return file with { DimensionReadError = "無法讀取圖片尺寸，請確認檔案內容是有效圖片" };
        }
    }

    private IReadOnlySet<string> GetExistingResizeTargetPaths(
        IReadOnlyList<FileItem> files,
        ResizeOptions options)
    {
        if (options.OutputMode != ResizeOutputMode.Subfolder)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var targetPaths = files
            .Select(file => Path.Combine(file.DirectoryPath, options.SubfolderName, file.Name))
            .ToList();

        return fileExistenceService.GetExistingPaths(targetPaths);
    }
}
