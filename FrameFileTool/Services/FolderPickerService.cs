using System.IO;
using Forms = System.Windows.Forms;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

public sealed class FolderPickerService : IFolderPickerService
{
    public string? PickFolder(string? initialFolder)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "選擇圖片資料夾",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrWhiteSpace(initialFolder) && Directory.Exists(initialFolder))
        {
            dialog.SelectedPath = initialFolder;
        }

        return dialog.ShowDialog() == Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }
}
