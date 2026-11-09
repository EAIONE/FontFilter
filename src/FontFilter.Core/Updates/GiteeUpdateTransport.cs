using System.Net.Http.Headers;
using System.Text.Json;

namespace FontFilter;

public sealed class GiteeUpdateTransport(string token) : DelegatingHandler(new HttpClientHandler())
{
    private const string Api = "https://gitee.com/api/v5/repos/orsone/FontFilter/releases";
    private readonly Dictionary<string, long> releaseIds = new(StringComparer.Ordinal);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Existing signed manifests name the original release URL. Use its tag
        // and filename to locate the identical attachment on Gitee.
        var uri = request.RequestUri;
        if (uri is null || uri.Scheme != "https" || uri.Host != "github.com")
            throw new IOException("更新附件地址无效。");
        var segments = uri.AbsolutePath.Split('/');
        if (segments.Length != 7 || segments[1] != "EAIONE" || segments[2] != "FontFilter"
            || segments[3] != "releases"
            || !(segments[4] == "download" || (segments[4] == "latest" && segments[5] == "download")))
            throw new IOException("更新附件地址无效。");
        var tag = segments[4] == "latest" ? "latest" : segments[5];
        var filename = segments[6];
        if (!releaseIds.TryGetValue(tag, out var releaseId))
        {
            releaseId = tag == "latest"
                ? await FindLatestStableRelease(cancellationToken)
                : await FindReleaseByTag(tag, cancellationToken);
            releaseIds.Add(tag, releaseId);
        }

        var attachmentsUrl = $"{Api}/{releaseId}/attach_files";
        using var attachments = await ReadJson(attachmentsUrl, cancellationToken);
        foreach (var attachment in attachments.RootElement.EnumerateArray())
        {
            if (attachment.GetProperty("name").GetString() != filename)
                continue;

            var id = attachment.GetProperty("id").GetInt64();
            using var download = AuthorizedRequest($"{attachmentsUrl}/{id}/download");
            return await base.SendAsync(download, cancellationToken);
        }

        throw new IOException($"Gitee 发行版缺少附件 {filename}");
    }

    private async Task<long> FindReleaseByTag(string tag, CancellationToken cancellationToken)
    {
        using var release = await ReadJson(Api + "/tags/" + Uri.EscapeDataString(tag), cancellationToken);
        return release.RootElement.GetProperty("id").GetInt64();
    }

    private async Task<long> FindLatestStableRelease(CancellationToken cancellationToken)
    {
        Version? latestVersion = null;
        long latestId = 0;
        for (int page = 1; ; page++)
        {
            using var releases = await ReadJson($"{Api}?per_page=100&page={page}", cancellationToken);
            foreach (var release in releases.RootElement.EnumerateArray())
            {
                var tag = release.GetProperty("tag_name").GetString();
                if (tag is null || !tag.StartsWith('v') || !UpdatePolicy.IsVersion(tag[1..])
                    || (release.TryGetProperty("prerelease", out var prerelease) && prerelease.ValueKind == JsonValueKind.True))
                    continue;

                var version = Version.Parse(tag[1..]);
                if (latestVersion is null || version > latestVersion)
                {
                    latestVersion = version;
                    latestId = release.GetProperty("id").GetInt64();
                }
            }
            if (releases.RootElement.GetArrayLength() < 100)
                break;
        }
        return latestId != 0 ? latestId : throw new IOException("Gitee 仓库没有正式发行版。");
    }

    private async Task<JsonDocument> ReadJson(string url, CancellationToken cancellationToken)
    {
        using var request = AuthorizedRequest(url);
        using var response = await base.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private HttpRequestMessage AuthorizedRequest(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
