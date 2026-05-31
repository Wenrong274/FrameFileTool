using FrameFileTool.Models;

namespace FrameFileTool.Services.Interfaces;

public interface IUpdateService
{
    public Task<UpdateInfo> CheckForUpdateAsync(CancellationToken token);
}
