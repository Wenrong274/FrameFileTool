namespace FrameFileTool.Models;

/// <summary>
/// 代表一個已掃描的圖片檔案，為 immutable record。
/// 由 <see cref="FrameFileTool.Services.FileScanner"/> 建立，
/// 傳入各 planner 作為輸入資料。
/// </summary>
public sealed record FileItem(
    string FullPath,
    string DirectoryPath,
    string Name,
    string Extension,
    long SizeBytes,
    int Width = 0,
    int Height = 0);
