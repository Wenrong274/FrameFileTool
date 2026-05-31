namespace FrameFileTool.Models;

/// <summary>
/// 批次縮放操作的所有參數，為 immutable record。
/// 由 ViewModel 建立並傳入 ResizePlanner 與 ImageResizeExecutor。
/// </summary>
public sealed record ResizeOptions(
    ResizeMode Mode,
    double ScaleFactor,
    int TargetWidth,
    int TargetHeight,
    bool KeepAspectRatio,
    ResizeOutputMode OutputMode,
    string TargetFolderPath,
    ResamplerType Resampler,
    bool DenoiseEnabled = false);
