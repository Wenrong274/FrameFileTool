using System.Windows;
using FrameFileTool.Services;
using FrameFileTool.ViewModels;

namespace FrameFileTool;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var scanner = new FileScanner();
        var frameDeletePlanner = new FrameDeletePlanner();
        var renamePlanner = new RenamePlanner();
        var executor = new FileOperationExecutor();
        var folderPicker = new FolderPickerService();

        DataContext = new MainViewModel(
            scanner,
            frameDeletePlanner,
            renamePlanner,
            executor,
            folderPicker);
    }
}
