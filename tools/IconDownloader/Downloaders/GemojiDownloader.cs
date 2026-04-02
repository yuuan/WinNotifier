using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using IconDownloader.Models;

namespace IconDownloader.Downloaders;

public class GemojiDownloader : IIconSetDownloader
{
    private const string RepoUrl = "https://github.com/github/gemoji";
    private const string LatestReleaseUrl = "https://api.github.com/repos/github/gemoji/releases/latest";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _httpClient;
    private string? _version;

    public string Name => "gemoji";
    public string Category => "mapping";
    public string NewVersion => _version ?? "(latest)";
    public string ExistingMetaPath(string outputDir) => Path.Combine(outputDir, "icons", "mappings", "gemoji.json");

    public GemojiDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DownloadResult> DownloadAsync(
        string outputDir,
        IProgress<DownloadProgress> progress,
        CancellationToken ct)
    {
        var mappingsDir = Path.Combine(outputDir, "icons", "mappings");
        Directory.CreateDirectory(mappingsDir);

        // Get latest release tag
        progress.Report(new DownloadProgress("gemoji の最新バージョンを確認中...", "", 0, 0));
        _version = await GetLatestVersionAsync(ct);

        // Download emoji.json
        progress.Report(new DownloadProgress("gemoji の emoji.json をダウンロード中...", "", 0, 0));
        var emojiJsonUrl = $"https://raw.githubusercontent.com/github/gemoji/{_version}/db/emoji.json";
        var json = await _httpClient.GetStringAsync(emojiJsonUrl, ct);
        var entries = JsonSerializer.Deserialize<List<GemojiEntry>>(json) ?? [];

        // Build mappings
        progress.Report(new DownloadProgress("マッピングを生成中...", "", 0, 0));
        var mappings = new Dictionary<string, string>();
        foreach (var entry in entries)
        {
            if (entry.Emoji is null) continue;
            foreach (var alias in entry.Aliases)
            {
                mappings.TryAdd(alias, entry.Emoji);
            }
        }

        // Write gemoji.json
        var meta = new MappingMeta
        {
            Name = "gemoji",
            Source = new SourceInfo { Repository = RepoUrl, Version = _version },
            DownloadedAt = DateTimeOffset.Now,
            Mappings = mappings
        };
        var metaJson = JsonSerializer.Serialize(meta, JsonOptions);
        metaJson = UnescapeSurrogatePairs(metaJson);
        await File.WriteAllTextAsync(Path.Combine(mappingsDir, "gemoji.json"), metaJson, Encoding.UTF8, ct);

        return new DownloadResult("gemoji", mappings.Count, 0);
    }

    private async Task<string> GetLatestVersionAsync(CancellationToken ct)
    {
        var json = await _httpClient.GetStringAsync(LatestReleaseUrl, ct);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("tag_name").GetString() ?? "master";
    }

    private static string UnescapeSurrogatePairs(string json)
    {
        return Regex.Replace(json, @"\\u[0-9A-Fa-f]{4}(\\u[0-9A-Fa-f]{4})?", match =>
        {
            try
            {
                return Regex.Unescape(match.Value);
            }
            catch
            {
                return match.Value;
            }
        });
    }

    private sealed record GemojiEntry(
        [property: JsonPropertyName("emoji")] string? Emoji,
        [property: JsonPropertyName("aliases")] List<string> Aliases
    );
}
