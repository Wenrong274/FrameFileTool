using System.IO;

namespace FrameFileTool.Services;

/// <summary>
/// 驗證使用者輸入是否可安全地當作單一檔名或資料夾名稱。
/// 這裡只處理字串規則，不接觸檔案系統。
/// </summary>
internal static class PathSafetyValidator
{
    public static bool IsSafeSingleDirectoryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        if (name is "." or "..")
        {
            return false;
        }

        return !Path.IsPathRooted(name) &&
            Path.GetFileName(name) == name &&
            !ContainsPathSeparator(name) &&
            name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    public static bool IsSafeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return !Path.IsPathRooted(name) &&
            Path.GetFileName(name) == name &&
            !ContainsPathSeparator(name) &&
            name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    private static bool ContainsPathSeparator(string value) =>
        value.Contains(Path.DirectorySeparatorChar) ||
        value.Contains(Path.AltDirectorySeparatorChar);
}
