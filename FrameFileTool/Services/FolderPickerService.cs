using System.IO;
using FrameFileTool.Services.Interfaces;
using Forms = System.Windows.Forms;

namespace FrameFileTool.Services;

/// <summary>
/// 開啟 Windows 系統資料夾選擇對話框。
/// 使用 <see cref="Forms.FolderBrowserDialog"/>，需要 UseWindowsForms=true。
/// </summary>
public sealed class FolderPickerService : IFolderPickerService
{
    /// <inheritdoc/>
    public string? PickFolder(string? initialFolder)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "選擇圖片資料夾",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };

        // 若有先前選擇的資料夾且仍存在，預設開啟在該位置
        if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder))
        {
            dialog.SelectedPath = initialFolder!;
        }

        return dialog.ShowDialog() == Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }
}
