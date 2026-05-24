namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 開啟系統資料夾選擇對話框，回傳使用者選擇的路徑；取消則回傳 null。
/// </summary>
public interface IFolderPickerService
{
    string? PickFolder(string? initialFolder);
}
