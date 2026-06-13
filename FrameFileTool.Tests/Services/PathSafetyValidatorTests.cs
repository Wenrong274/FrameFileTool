using FluentAssertions;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

/// <summary>
/// <see cref="PathSafetyValidator"/> 的直接單元測試。
/// 此驗證器是改名與複製操作的檔名安全最後防線，
/// 測試只驗證字串規則，不接觸實際檔案系統。
/// 測試以 Windows 路徑語意為準（專案目標平台為 net-windows）。
/// </summary>
public sealed class PathSafetyValidatorTests
{
    // ── IsSafeFileName：正常路徑 ──────────────────────────────

    [Theory]
    [InlineData("frame001.png")]
    [InlineData("Symbol_0001.jpg")]
    [InlineData("中文檔名.webp")]
    [InlineData("name with space.png")]
    [InlineData("no-extension")]
    public void IsSafeFileName_合法單一檔名應回傳true(string name)
    {
        PathSafetyValidator.IsSafeFileName(name).Should().BeTrue();
    }

    // ── IsSafeFileName：空白輸入 ──────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void IsSafeFileName_空白輸入應回傳false(string name)
    {
        PathSafetyValidator.IsSafeFileName(name).Should().BeFalse();
    }

    // ── IsSafeFileName：rooted path 與路徑分隔符 ─────────────

    [Theory]
    [InlineData(@"C:\x.png")]      // 絕對路徑
    [InlineData(@"\x.png")]        // 磁碟相對的 rooted path
    [InlineData("/x.png")]         // alt 分隔符開頭的 rooted path
    [InlineData(@"a\b.png")]       // 內含路徑分隔符
    [InlineData("a/b.png")]        // 內含 alt 路徑分隔符
    [InlineData(@"..\x.png")]      // 含分隔符的相對跳脫
    public void IsSafeFileName_含路徑成分應回傳false(string name)
    {
        PathSafetyValidator.IsSafeFileName(name).Should().BeFalse();
    }

    // ── IsSafeFileName：Windows 非法字元 ─────────────────────

    [Theory]
    [InlineData("a:b.png")]
    [InlineData("a?b.png")]
    [InlineData("a*b.png")]
    [InlineData("a<b.png")]
    [InlineData("a>b.png")]
    [InlineData("a\"b.png")]
    [InlineData("a|b.png")]
    [InlineData("a\tb.png")] // 控制字元
    public void IsSafeFileName_含非法字元應回傳false(string name)
    {
        PathSafetyValidator.IsSafeFileName(name).Should().BeFalse();
    }

    // ── IsSafeFileName：已知行為記錄（路徑跳脫與邊界） ───────

    // 記錄目前行為：「..」不含分隔符與非法字元，驗證器會放行。
    // 實際的 rename/copy 以 Path.Combine(directory, "..") 組合時會指向上層目錄，
    // 屬於已知限制；若未來要封鎖，應在此改為 BeFalse 並同步修改驗證器。
    [Fact]
    public void IsSafeFileName_點點輸入_記錄目前放行行為()
    {
        PathSafetyValidator.IsSafeFileName("..").Should().BeTrue();
    }

    // 記錄目前行為：尾端點號或空白（Windows 會默默截斷）不在驗證範圍，會放行。
    [Theory]
    [InlineData("name.")]
    [InlineData("name.png ")]
    public void IsSafeFileName_尾端點號或空白_記錄目前放行行為(string name)
    {
        PathSafetyValidator.IsSafeFileName(name).Should().BeTrue();
    }

    // 記錄目前行為：驗證器不檢查檔名長度，超長檔名交由檔案系統在執行時回報錯誤。
    [Fact]
    public void IsSafeFileName_超長檔名_記錄目前放行行為()
    {
        var name = new string('a', 300) + ".png";

        PathSafetyValidator.IsSafeFileName(name).Should().BeTrue();
    }

    // ── IsSafeTargetDirectoryPath：正常路徑 ──────────────────

    [Theory]
    [InlineData(@"C:\output")]
    [InlineData(@"C:\output\sub")]
    [InlineData(@"D:\圖片輸出")]
    public void IsSafeTargetDirectoryPath_合法絕對路徑應回傳true(string path)
    {
        PathSafetyValidator.IsSafeTargetDirectoryPath(path).Should().BeTrue();
    }

    // 記錄目前行為：前後空白會先 Trim 再驗證。
    [Fact]
    public void IsSafeTargetDirectoryPath_前後空白應先修剪再驗證()
    {
        PathSafetyValidator.IsSafeTargetDirectoryPath(@"  C:\output  ").Should().BeTrue();
    }

    // 記錄目前行為：UNC 路徑屬於 fully qualified，會放行。
    [Fact]
    public void IsSafeTargetDirectoryPath_UNC路徑_記錄目前放行行為()
    {
        PathSafetyValidator.IsSafeTargetDirectoryPath(@"\\server\share\output").Should().BeTrue();
    }

    // ── IsSafeTargetDirectoryPath：不安全輸入 ────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("output")]          // 相對路徑
    [InlineData(@"output\sub")]     // 相對路徑
    [InlineData(@"..\output")]      // 相對跳脫
    [InlineData(@"\output")]        // 磁碟相對 root，非 fully qualified
    [InlineData("/output")]         // alt 分隔符開頭，非 fully qualified
    [InlineData("C:output")]        // 磁碟相對路徑，非 fully qualified
    [InlineData("C:\\out|put")]     // Windows 非法路徑字元
    [InlineData("C:\\out\tput")]    // 控制字元
    public void IsSafeTargetDirectoryPath_不安全輸入應回傳false(string path)
    {
        PathSafetyValidator.IsSafeTargetDirectoryPath(path).Should().BeFalse();
    }

    // ── 不拋例外保證 ─────────────────────────────────────────

    // 兩個方法對任何字串輸入都不應拋出例外，只能回傳 true/false。
    [Theory]
    [InlineData("\0")]
    [InlineData("C:\\a\0b")]
    [InlineData("::::")]
    public void 任何輸入都不應拋出例外(string input)
    {
        var fileNameAct = () => PathSafetyValidator.IsSafeFileName(input);
        var directoryAct = () => PathSafetyValidator.IsSafeTargetDirectoryPath(input);

        fileNameAct.Should().NotThrow();
        directoryAct.Should().NotThrow();
    }
}
