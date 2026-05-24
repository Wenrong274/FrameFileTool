using System.Windows;
using WpfApplication = System.Windows.Application;
using FrameFileTool.Services;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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
