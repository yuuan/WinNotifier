using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using IconDownloader.Models;

namespace IconDownloader.Downloaders;

public class TwemojiDownloader : IIconSetDownloader
{
    private const string RepoUrl = "https://github.com/jdecked/twemoji";
    private const string Version = "v17.0.2";
    private const string ArchiveUrl = $"https://github.com/jdecked/twemoji/archive/refs/tags/{Version}.zip";
    private const string LicenseUrl = $"https://raw.githubusercontent.com/jdecked/twemoji/{Version}/LICENSE";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _httpClient;

    public string Name => "Twemoji";
    public string Category => "theme";
    public string NewVersion => Version;
    public string ExistingMetaPath(string outputDir) => Path.Combine(outputDir, "icons", "themes", Name, "meta.json");

    public TwemojiDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DownloadResult> DownloadAsync(
        string outputDir,
        IProgress<DownloadProgress> progress,
        CancellationToken ct)
    {
        var themeDir = Path.Combine(outputDir, "icons", "themes", "Twemoji");
        var assetsDir = Path.Combine(themeDir, "assets");

        if (Directory.Exists(themeDir))
            Directory.Delete(themeDir, true);

        Directory.CreateDirectory(assetsDir);

        // Download ZIP
        progress.Report(new DownloadProgress("Twemoji アーカイブをダウンロード中...", "", 0, 0));

        var tempZip = Path.GetTempFileName();
        try
        {
            using (var response = await _httpClient.GetAsync(ArchiveUrl, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                await using var fileStream = File.Create(tempZip);
                await stream.CopyToAsync(fileStream, ct);
            }

            // Extract PNGs
            progress.Report(new DownloadProgress("PNG を展開中...", "", 0, 0));

            var icons = new List<IconEntry>();
            using var zip = ZipFile.OpenRead(tempZip);

            var pngEntries = zip.Entries
                .Where(e => e.FullName.Contains("/assets/72x72/") && e.Name.EndsWith(".png"))
                .ToList();

            for (int i = 0; i < pngEntries.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var entry = pngEntries[i];
                var fileName = entry.Name;
                var destPath = Path.Combine(assetsDir, fileName);

                entry.ExtractToFile(destPath, overwrite: true);

                var name = Path.GetFileNameWithoutExtension(fileName);
                icons.Add(new IconEntry
                {
                    Name = name,
                    File = Path.Combine("assets", fileName),
                    Aliases = [CodepointsToEmoji(name)]
                });

                if (i % 100 == 0 || i == pngEntries.Count - 1)
                {
                    progress.Report(new DownloadProgress(
                        "PNG を展開中...",
                        $"{i + 1} / {pngEntries.Count}",
                        i + 1,
                        pngEntries.Count));
                }
            }

            // Download LICENSE
            progress.Report(new DownloadProgress("LICENSE をダウンロード中...", "", 0, 0));
            var license = await _httpClient.GetStringAsync(LicenseUrl, ct);
            await File.WriteAllTextAsync(Path.Combine(themeDir, "LICENSE"), license, ct);

            // Write meta.json
            var meta = new ThemeMeta
            {
                Theme = "Twemoji",
                Source = new SourceInfo { Repository = RepoUrl, Version = Version },
                DownloadedAt = DateTimeOffset.Now,
                Icons = icons
            };
            var metaJson = JsonSerializer.Serialize(meta, JsonOptions);
            metaJson = UnescapeSurrogatePairs(metaJson);
            await File.WriteAllTextAsync(Path.Combine(themeDir, "meta.json"), metaJson, Encoding.UTF8, ct);

            return new DownloadResult("Twemoji", icons.Count, 0);
        }
        finally
        {
            if (File.Exists(tempZip))
                File.Delete(tempZip);
        }
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

    private static string CodepointsToEmoji(string codepoints)
    {
        return string.Concat(
            codepoints.Split('-')
                .Select(cp => char.ConvertFromUtf32(Convert.ToInt32(cp, 16))));
    }
}
