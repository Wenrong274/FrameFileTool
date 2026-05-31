using FluentAssertions;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class ExternalLinkServiceTests
{
    [Theory]
    [InlineData("https://github.com/example/releases/tag/v1.3.0")]
    [InlineData("http://example.com")]
    public void Open_合法絕對Url_不應丟出例外(string url)
    {
        var sut = new ExternalLinkService();

        var act = () => sut.Open(url);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    [InlineData("   ")]
    public void Open_非合法絕對Url_應靜默忽略不丟出例外(string url)
    {
        var sut = new ExternalLinkService();

        var act = () => sut.Open(url);

        act.Should().NotThrow();
    }
}
