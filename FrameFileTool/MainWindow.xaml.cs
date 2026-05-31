using System.Windows;
using FrameFileTool.ViewModels;

namespace FrameFileTool;

public partial class MainWindow : Window
{
    private DenoiseCompareWindow? _denoiseCompareWindow;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.DenoisePreviewGenerated += OpenDenoiseCompareWindow;
    }

    private void PreviewDropTarget_DragEnter(object sender, System.Windows.DragEventArgs e) =>
        UpdatePreviewDropState(e, isActive: true);

    private void PreviewDropTarget_DragOver(object sender, System.Windows.DragEventArgs e) =>
        UpdatePreviewDropState(e, isActive: true);

    private void PreviewDropTarget_DragLeave(object sender, System.Windows.DragEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.IsPreviewDropTargetActive = false;
        }

        e.Handled = true;
    }

    private void PreviewDropTarget_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            e.Handled = true;
            return;
        }

        viewModel.IsPreviewDropTargetActive = false;

        var paths = GetDroppedPaths(e);
        if (paths.Length > 0 && viewModel.ImportDroppedPathsCommand.CanExecute(paths))
        {
            viewModel.ImportDroppedPathsCommand.Execute(paths);
        }

        e.Handled = true;
    }

    private void UpdatePreviewDropState(System.Windows.DragEventArgs e, bool isActive)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            e.Effects = System.Windows.DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var paths = GetDroppedPaths(e);
        var canDrop = paths.Length > 0 && viewModel.ImportDroppedPathsCommand.CanExecute(paths);
        e.Effects = canDrop ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        viewModel.IsPreviewDropTargetActive = isActive && canDrop;
        e.Handled = true;
    }

    private void OpenDenoiseCompare_Click(object sender, RoutedEventArgs e)
    {
        if (_denoiseCompareWindow is { IsLoaded: true })
        {
            _denoiseCompareWindow.Activate();
            return;
        }

        OpenDenoiseCompareWindow();
    }

    private void OpenDenoiseCompareWindow()
    {
        if (DataContext is not MainViewModel vm)
            return;

        _denoiseCompareWindow?.Close();
        _denoiseCompareWindow = new DenoiseCompareWindow(
            vm.DenoisePreviewDetail,
            vm.DenoisePreviewStandard,
            vm.DenoisePreviewStrong)
        {
            Owner = this,
        };
        _denoiseCompareWindow.Show();
    }

    private static string[] GetDroppedPaths(System.Windows.DragEventArgs e) =>
        e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) &&
        e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths
            ? paths
            : [];
}
