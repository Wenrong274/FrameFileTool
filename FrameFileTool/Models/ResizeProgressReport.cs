namespace FrameFileTool.Models;

/// <summary>
/// 單次進度回報資料，由 <see cref="FrameFileTool.Services.ImageResizeExecutor"/> 與
/// <see cref="FrameFileTool.Services.DenoiseExecutor"/> 透過
/// <see cref="IProgress{T}"/> 傳回給 ViewModel，再更新 UI 進度條。
/// </summary>
public sealed record ResizeProgressReport(
    int Current,
    int Total,
    string CurrentFileName);
