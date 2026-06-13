namespace FrameFileTool.Models;

/// <summary>
/// 批次降噪操作的所有參數，為 immutable record。
/// 由 ViewModel 建立並傳入 DenoisePlanner 與 DenoiseExecutor。
/// 批次降噪僅支援覆寫原檔，因此不含輸出位置設定。
/// </summary>
public sealed record DenoiseOptions(DenoiseMode Mode);
