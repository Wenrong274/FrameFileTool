namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 讀取圖片寬高資訊。只讀取檔案標頭，不解碼完整影像，效能接近 I/O 上限。
/// 供 ViewModel 在縮放預覽前批次取得原始尺寸，再傳入 ResizePlanner 計算目標尺寸。
/// </summary>
public interface IImageDimensionReader
{
    /// <summary>
    /// 讀取指定圖片的寬度與高度（像素）。
    /// 若檔案不存在或格式不支援，會拋出例外；呼叫端應自行處理。
    /// </summary>
    public (int Width, int Height) Read(string filePath);
}
