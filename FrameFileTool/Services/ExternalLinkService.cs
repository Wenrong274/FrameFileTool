using System.Diagnostics;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

public sealed class ExternalLinkService : IExternalLinkService
{
    public void Open(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true,
        });
    }
}
