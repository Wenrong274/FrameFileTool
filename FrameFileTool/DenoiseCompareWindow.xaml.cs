using System.Windows;
using System.Windows.Media.Imaging;

namespace FrameFileTool;

public partial class DenoiseCompareWindow : Window
{
    public DenoiseCompareWindow(
        BitmapSource? detail,
        BitmapSource? standard,
        BitmapSource? strong)
    {
        InitializeComponent();
        DetailImage.Source = detail;
        StandardImage.Source = standard;
        StrongImage.Source = strong;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
            Close();

        base.OnKeyDown(e);
    }
}
