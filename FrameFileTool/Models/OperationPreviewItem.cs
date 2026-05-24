namespace FrameFileTool.Models;

public sealed class OperationPreviewItem
{
    public int Index { get; init; }
    public string FullPath { get; init; } = string.Empty;
    public string OriginalName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string TargetName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool HasError { get; init; }
}
