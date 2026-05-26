using System.Windows;
using FrameFileTool.Services;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication = System.Windows.Application;

namespace FrameFileTool;

public partial class App : WpfApplication
{
    private readonly IServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<IFileScanner, FileScanner>();
        services.AddSingleton<IFrameDeletePlanner, FrameDeletePlanner>();
        services.AddSingleton<IRenamePlanner, RenamePlanner>();
        services.AddSingleton<IFileOperationExecutor, FileOperationExecutor>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddSingleton<IResizePlanner, ResizePlanner>();
        services.AddSingleton<IImageResizeExecutor, ImageResizeExecutor>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _serviceProvider.GetRequiredService<MainWindow>().Show();
    }
}
