using System.Diagnostics;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

public sealed class ExternalLinkService : IExternalLinkService
{
    private readonly Action<string> _launch;

    public ExternalLinkService() => _launch = DefaultLaunch;

    // 供測試注入，避免實際開啟瀏覽器
    internal ExternalLinkService(Action<string> launch) => _launch = launch;

    public void Open(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            return;

        _launch(url);
    }

    private static void DefaultLaunch(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
