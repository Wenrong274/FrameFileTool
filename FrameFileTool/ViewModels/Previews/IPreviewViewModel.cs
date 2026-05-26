namespace FrameFileTool.ViewModels.Previews;

/// <summary>
/// 預覽結果 ViewModel 的共用介面。
/// MainViewModel 將 <see cref="CurrentPreview"/> 指派為此介面的某個實作，
/// 摘要列的綁定與 ContentControl 的 DataTemplate 選擇均依賴此介面。
/// </summary>
public interface IPreviewViewModel
{
    /// <summary>顯示於預覽摘要列的說明文字。</summary>
    public string Summary { get; }

    /// <summary>是否含有任何錯誤項目。</summary>
    public bool HasErrors { get; }
}
