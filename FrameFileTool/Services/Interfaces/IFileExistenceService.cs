namespace FrameFileTool.Services.Interfaces;

/// <summary>
/// 查詢一組路徑目前是否存在，讓 planner 可使用已知快照而不直接碰檔案系統。
/// </summary>
public interface IFileExistenceService
{
    public IReadOnlySet<string> GetExistingPaths(IEnumerable<string> paths);
}
