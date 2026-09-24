using System.Text.Json.Serialization;

namespace Rysy.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public sealed record GitHubRelease(
    string? Version,
    string? Description,
    IReadOnlyList<GitHubAsset> Assets);

public sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}

public static class GitHubApi
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private static readonly JsonSerializerOptions JsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        // GitHub requires a User-Agent header.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Rysy/1.0");

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        // Current GitHub API version.
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");

        return client;
    }

    public static async Task<GitHubRelease?> GetLatestReleaseAsync(
        string repositoryUrl,
        string? githubToken = null,
        CancellationToken cancellationToken = default)
    {
        var (owner, repo) = ParseRepositoryUrl(repositoryUrl);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/releases/latest");

        if (!string.IsNullOrWhiteSpace(githubToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
        }

        using var response = await HttpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        // No published release exists, or the repository could not be found.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var release = await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(
            stream,
            JsonSerializerOptions,
            cancellationToken);

        if (release is null)
            return null;

        return new GitHubRelease(
            release.Name,
            release.Body,
            release.Assets?.ToList() ?? []);
    }

    private static (string Owner, string Repo) ParseRepositoryUrl(string repositoryUrl)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Expected a GitHub repository URL such as https://github.com/dotnet/runtime", nameof(repositoryUrl));
        }

        var parts = uri.AbsolutePath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
            throw new ArgumentException("The URL must contain an owner and repository name.", nameof(repositoryUrl));

        var owner = parts[0];
        var repo = parts[1];

        if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repo = repo[..^4];

        return (owner, repo);
    }

    private sealed class GitHubReleaseResponse
    {
        public string? Name { get; set; }
        public string? Body { get; set; }
        public List<GitHubAsset>? Assets { get; set; }
    }
}
