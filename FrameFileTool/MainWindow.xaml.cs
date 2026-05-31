using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using FrameFileTool.Models;
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

    private void OpenDenoiseCompareWindow(DenoisePreviewResult result)
    {
        _denoiseCompareWindow?.Close();
        _denoiseCompareWindow = new DenoiseCompareWindow(
            ToBitmapSource(result.PreviewsByMode.GetValueOrDefault(DenoiseMode.Detail)),
            ToBitmapSource(result.PreviewsByMode.GetValueOrDefault(DenoiseMode.Standard)),
            ToBitmapSource(result.PreviewsByMode.GetValueOrDefault(DenoiseMode.Strong)))
        {
            Owner = this,
        };
        _denoiseCompareWindow.Show();
    }

    private static System.Windows.Media.Imaging.BitmapSource? ToBitmapSource(byte[]? data)
    {
        if (data is null)
            return null;

        using var stream = new MemoryStream(data);
        var bi = new BitmapImage();
        bi.BeginInit();
        bi.StreamSource = stream;
        bi.CacheOption = BitmapCacheOption.OnLoad;
        bi.EndInit();
        bi.Freeze();
        return bi;
    }

    private static string[] GetDroppedPaths(System.Windows.DragEventArgs e) =>
        e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) &&
        e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] paths
            ? paths
            : [];
}
