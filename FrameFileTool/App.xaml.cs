using System.Net.Http;
using System.Windows;
using FrameFileTool.Services;
using FrameFileTool.Services.Interfaces;
using FrameFileTool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;

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
        services.AddSingleton<IFileImportService, FileImportService>();
        services.AddSingleton<IResizePlanner, ResizePlanner>();
        services.AddSingleton<IResizePreviewService, ResizePreviewService>();
        services.AddSingleton<IOutputFolderResolver, OutputFolderResolver>();
        services.AddSingleton<IImageResizeExecutor, ImageResizeExecutor>();
        services.AddSingleton<IDenoisePlanner, DenoisePlanner>();
        services.AddSingleton<IDenoiseExecutor, DenoiseExecutor>();
        services.AddSingleton<IDenoisePreviewService, DenoisePreviewService>();
        services.AddSingleton<IImageDimensionReader, ImageDimensionReader>();
        services.AddSingleton<IFileExistenceService, FileExistenceService>();
        services.AddSingleton<HttpClient>(_ => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5),
        });
        services.AddSingleton<IUpdateService>(provider =>
        {
            var version = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0);
            return new GitHubUpdateService(provider.GetRequiredService<HttpClient>(), version);
        });
        services.AddSingleton<IExternalLinkService, ExternalLinkService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        _serviceProvider.GetRequiredService<MainWindow>().Show();
    }

    /// <summary>
    /// UI 執行緒上未處理的例外預設會直接終止程式，使用者只看到閃退。
    /// 這裡改為顯示可回報的錯誤內容並讓程式繼續執行。
    /// </summary>
    private static void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        WpfMessageBox.Show(
            $"發生未預期的錯誤，操作已中止：\n\n{e.Exception.GetType().Name}\n{e.Exception.Message}",
            "影格整理工具",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        (_serviceProvider as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}
