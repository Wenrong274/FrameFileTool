namespace FrameFileTool.Models;

public sealed record UpdateInfo(bool HasUpdate, string LatestVersion, string ReleaseUrl)
{
    public static UpdateInfo None { get; } = new(false, string.Empty, string.Empty);
}
