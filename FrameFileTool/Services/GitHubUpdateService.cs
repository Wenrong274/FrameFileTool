using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using FrameFileTool.Models;
using FrameFileTool.Services.Interfaces;

namespace FrameFileTool.Services;

public sealed class GitHubUpdateService : IUpdateService
{
    private static readonly Uri _latestReleaseUri = new("https://api.github.com/repos/Wenrong274/FrameFileTool/releases/latest");

    private readonly HttpClient _httpClient;
    private readonly Version _currentVersion;

    public GitHubUpdateService(HttpClient httpClient, Version currentVersion)
    {
        _httpClient = httpClient;
        _currentVersion = Normalize(currentVersion);
    }

    public async Task<UpdateInfo> CheckForUpdateAsync(CancellationToken token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _latestReleaseUri);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("FrameFileTool", "1.0"));

            using var response = await _httpClient.SendAsync(request, token);
            if (!response.IsSuccessStatusCode)
            {
                return UpdateInfo.None;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token);

            if (!TryReadRelease(document.RootElement, out var latestVersionText, out var releaseUrl)
                || !TryParseVersion(latestVersionText, out var latestVersion)
                || Normalize(latestVersion).CompareTo(_currentVersion) <= 0)
            {
                return UpdateInfo.None;
            }

            return new UpdateInfo(true, latestVersionText, releaseUrl);
        }
        catch (OperationCanceledException)
        {
            return UpdateInfo.None;
        }
        catch (HttpRequestException)
        {
            return UpdateInfo.None;
        }
        catch (JsonException)
        {
            return UpdateInfo.None;
        }
    }

    private static bool TryReadRelease(JsonElement root, out string latestVersion, out string releaseUrl)
    {
        latestVersion = string.Empty;
        releaseUrl = string.Empty;

        if (!root.TryGetProperty("tag_name", out var tagElement)
            || !root.TryGetProperty("html_url", out var urlElement))
        {
            return false;
        }

        if (tagElement.ValueKind != JsonValueKind.String
            || urlElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        latestVersion = tagElement.GetString() ?? string.Empty;
        releaseUrl = urlElement.GetString() ?? string.Empty;

        return !string.IsNullOrWhiteSpace(latestVersion)
            && Uri.TryCreate(releaseUrl, UriKind.Absolute, out _);
    }

    private static bool TryParseVersion(string versionText, out Version version)
    {
        var normalizedText = versionText.Trim();
        if (normalizedText.StartsWith('v') || normalizedText.StartsWith('V'))
        {
            normalizedText = normalizedText[1..];
        }

        var prereleaseIndex = normalizedText.IndexOf('-', StringComparison.Ordinal);
        if (prereleaseIndex >= 0)
        {
            normalizedText = normalizedText[..prereleaseIndex];
        }

        return Version.TryParse(normalizedText, out version!);
    }

    private static Version Normalize(Version version) =>
        new(
            version.Major,
            version.Minor,
            Math.Max(version.Build, 0),
            Math.Max(version.Revision, 0));
}
