using System.Net;
using FluentAssertions;
using FrameFileTool.Services;

namespace FrameFileTool.Tests.Services;

public sealed class GitHubUpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdateAsync_遠端版本較新_應回傳可更新與ReleaseUrl()
    {
        var sut = CreateSut(
            new Version(1, 2, 0),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""{"tag_name":"v1.3.0","html_url":"https://github.com/example/releases/tag/v1.3.0"}"""),
            });

        var result = await sut.CheckForUpdateAsync(CancellationToken.None);

        result.HasUpdate.Should().BeTrue();
        result.LatestVersion.Should().Be("v1.3.0");
        result.ReleaseUrl.Should().Be("https://github.com/example/releases/tag/v1.3.0");
    }

    [Fact]
    public async Task CheckForUpdateAsync_遠端版本相同_不應顯示更新()
    {
        var sut = CreateSut(
            new Version(1, 3, 0),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""{"tag_name":"v1.3.0","html_url":"https://github.com/example/releases/tag/v1.3.0"}"""),
            });

        var result = await sut.CheckForUpdateAsync(CancellationToken.None);

        result.HasUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdateAsync_遠端版本較舊_不應顯示更新()
    {
        var sut = CreateSut(
            new Version(1, 4, 0),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""{"tag_name":"v1.3.0","html_url":"https://github.com/example/releases/tag/v1.3.0"}"""),
            });

        var result = await sut.CheckForUpdateAsync(CancellationToken.None);

        result.HasUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdateAsync_Api回傳非成功狀態_應靜默回傳無更新()
    {
        var sut = CreateSut(
            new Version(1, 2, 0),
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await sut.CheckForUpdateAsync(CancellationToken.None);

        result.HasUpdate.Should().BeFalse();
        result.LatestVersion.Should().BeEmpty();
        result.ReleaseUrl.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckForUpdateAsync_Json格式不符_應靜默回傳無更新()
    {
        var sut = CreateSut(
            new Version(1, 2, 0),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""{"name":"broken"}"""),
            });

        var result = await sut.CheckForUpdateAsync(CancellationToken.None);

        result.HasUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdateAsync_Json欄位型別不符_應靜默回傳無更新()
    {
        var sut = CreateSut(
            new Version(1, 2, 0),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent("""{"tag_name":123,"html_url":false}"""),
            });

        var result = await sut.CheckForUpdateAsync(CancellationToken.None);

        result.HasUpdate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckForUpdateAsync_網路逾時或失敗_應靜默回傳無更新()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new TaskCanceledException("timeout"));
        using var httpClient = new HttpClient(handler);
        var sut = new GitHubUpdateService(httpClient, new Version(1, 2, 0));

        var result = await sut.CheckForUpdateAsync(CancellationToken.None);

        result.HasUpdate.Should().BeFalse();
    }

    private static GitHubUpdateService CreateSut(Version currentVersion, HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(_ => response);
        var httpClient = new HttpClient(handler);
        return new GitHubUpdateService(httpClient, currentVersion);
    }

    private static StringContent JsonContent(string json) =>
        new(json, System.Text.Encoding.UTF8, "application/json");

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(send(request));
    }
}
