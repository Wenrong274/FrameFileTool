using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 將使用者選擇的輸出資料夾解析成實際安全輸出位置。
/// </summary>
public interface IOutputFolderResolver
{
    /// <summary>解析批次縮放的實際輸出資料夾。</summary>
    public ResolvedOutputFolder ResolveForResize(
        string sourceFolderPath,
        string selectedTargetFolderPath,
        ResizeOptions options);
}
