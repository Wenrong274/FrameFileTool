using FluentAssertions;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class ExternalLinkServiceTests
{
    [Theory]
    [InlineData("https://github.com/example/releases/tag/v1.3.0")]
    [InlineData("http://example.com")]
    public void Open_合法絕對Url_應呼叫Shell開啟(string url)
    {
        var launched = new List<string>();
        var sut = new ExternalLinkService(u => launched.Add(u));

        sut.Open(url);

        launched.Should().ContainSingle().Which.Should().Be(url);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("relative/path")]
    [InlineData("   ")]
    public void Open_非合法絕對Url_應靜默忽略不丟出例外(string url)
    {
        var launched = new List<string>();
        var sut = new ExternalLinkService(u => launched.Add(u));

        var act = () => sut.Open(url);

        act.Should().NotThrow();
        launched.Should().BeEmpty();
    }
}
