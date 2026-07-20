using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace FiveQC.ClientInstaller;

internal sealed class GitHubService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public GitHubService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("FiveQC-Client-Installer", GetCurrentVersion().ToString(3)));
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task<RemoteConfig> GetRemoteConfigAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(AppConstants.RemoteConfigUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        RemoteConfig? config = await JsonSerializer.DeserializeAsync<RemoteConfig>(stream, _jsonOptions, cancellationToken);
        return config ?? throw new InvalidDataException("La configuration distante est vide ou invalide.");
    }

    public async Task<GitHubRelease> GetLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(AppConstants.LatestReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        GitHubRelease? release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, _jsonOptions, cancellationToken);
        return release ?? throw new InvalidDataException("La réponse GitHub ne contient aucune release valide.");
    }

    public async Task DownloadFileAsync(
        string url,
        string destination,
        IProgress<int>? progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        long? length = response.Content.Headers.ContentLength;
        await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream output = new(destination, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 128, useAsync: true);

        byte[] buffer = new byte[1024 * 128];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            total += read;
            if (length is > 0)
                progress?.Report((int)Math.Clamp(total * 100L / length.Value, 0, 100));
        }

        progress?.Report(100);
    }


    public async Task<string?> ResolveAssetDigestAsync(
        GitHubRelease release,
        GitHubAsset asset,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(asset.Digest))
            return asset.Digest;

        GitHubAsset? checksumAsset = release.Assets.FirstOrDefault(candidate =>
            candidate.Name.Equals(asset.Name + ".sha256", StringComparison.OrdinalIgnoreCase));
        if (checksumAsset is null)
            return null;

        using HttpResponseMessage response = await _httpClient.GetAsync(checksumAsset.BrowserDownloadUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        string text = await response.Content.ReadAsStringAsync(cancellationToken);
        string hash = text.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return hash.Length == 64 ? "sha256:" + hash : null;
    }

    public static Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);
    }

    public static bool IsReleaseNewer(string tagName)
    {
        string normalized = tagName.Trim().TrimStart('v', 'V');
        int dash = normalized.IndexOf('-');
        if (dash >= 0)
            normalized = normalized[..dash];

        return Version.TryParse(normalized, out Version? remote) && remote > GetCurrentVersion();
    }

    public static async Task VerifySha256Async(string filePath, string? digest, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(digest))
            return;

        string expected = digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)
            ? digest[7..]
            : digest;

        await using FileStream stream = File.OpenRead(filePath);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        string actual = Convert.ToHexString(hash);

        if (!actual.Equals(expected.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("La vérification SHA-256 du téléchargement a échoué.");
    }

    public void Dispose() => _httpClient.Dispose();
}
