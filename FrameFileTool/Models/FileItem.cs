namespace FrameFileTool.Models;

public sealed record FileItem(
    string FullPath,
    string DirectoryPath,
    string Name,
    string Extension,
    long SizeBytes);
