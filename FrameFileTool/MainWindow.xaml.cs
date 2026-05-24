using System.Windows;
using FrameFileTool.ViewModels;

namespace FrameFileTool;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
