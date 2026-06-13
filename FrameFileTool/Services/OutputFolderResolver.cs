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
        ResizeOptions options) =>
        new(selectedTargetFolderPath, WasAutoRedirected: false, LogMessage: null);
}
