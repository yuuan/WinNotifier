using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using IconDownloader.Models;
using SkiaSharp;
using Svg.Skia;

namespace IconDownloader.Downloaders;

public class YaruDownloader : IIconSetDownloader
{
    private const string RepoUrl = "https://github.com/ubuntu/yaru";
    private const string ArchiveUrl = "https://github.com/ubuntu/yaru/archive/refs/heads/master.zip";
    private const string LicenseUrl = "https://raw.githubusercontent.com/ubuntu/yaru/master/COPYING";
    private const int RenderSize = 72;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly HttpClient _httpClient;

    public string Name => "Yaru";
    public string NewVersion => $"master ({DateTime.Now:yyyy-MM-dd})";
    public string ExistingMetaPath(string outputDir) => Path.Combine(outputDir, "icons", "themes", Name, "meta.json");

    public YaruDownloader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DownloadResult> DownloadAsync(
        string outputDir,
        IProgress<DownloadProgress> progress,
        CancellationToken ct)
    {
        var themeDir = Path.Combine(outputDir, "icons", "themes", "Yaru");
        var assetsDir = Path.Combine(themeDir, "assets");

        if (Directory.Exists(themeDir))
            Directory.Delete(themeDir, true);

        Directory.CreateDirectory(assetsDir);

        // Download ZIP
        progress.Report(new DownloadProgress("Yaru アーカイブをダウンロード中...", "", 0, 0));

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

            // Find fullcolor SVGs
            progress.Report(new DownloadProgress("SVG を検索中...", "", 0, 0));

            using var zip = ZipFile.OpenRead(tempZip);
            var svgEntries = zip.Entries
                .Where(e => e.FullName.Contains("/icons/src/fullcolor/") && e.Name.EndsWith(".svg"))
                .ToList();

            var icons = new List<IconEntry>();
            int skipped = 0;
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < svgEntries.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var entry = svgEntries[i];
                var baseName = Path.GetFileNameWithoutExtension(entry.Name);

                if (!seenNames.Add(baseName))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    // Save SVG
                    var svgPath = Path.Combine(assetsDir, $"{baseName}.svg");
                    entry.ExtractToFile(svgPath, overwrite: true);

                    // Render to PNG
                    var pngPath = Path.Combine(assetsDir, $"{baseName}.png");
                    RenderSvgToPng(svgPath, pngPath);

                    icons.Add(new IconEntry
                    {
                        Name = baseName,
                        File = Path.Combine("assets", $"{baseName}.png")
                    });
                }
                catch
                {
                    skipped++;
                }

                if (i % 50 == 0 || i == svgEntries.Count - 1)
                {
                    progress.Report(new DownloadProgress(
                        "SVG を PNG に変換中...",
                        $"{i + 1} / {svgEntries.Count}",
                        i + 1,
                        svgEntries.Count));
                }
            }

            // Download LICENSE
            progress.Report(new DownloadProgress("LICENSE をダウンロード中...", "", 0, 0));
            var license = await _httpClient.GetStringAsync(LicenseUrl, ct);
            await File.WriteAllTextAsync(Path.Combine(themeDir, "LICENSE"), license, ct);

            // Resolve version from ZIP (use commit date as proxy)
            var version = $"master ({DateTime.Now:yyyy-MM-dd})";

            // Write meta.json
            var meta = new ThemeMeta
            {
                Theme = "Yaru",
                Source = new SourceInfo { Repository = RepoUrl, Version = version },
                DownloadedAt = DateTimeOffset.Now,
                Icons = icons
            };
            var metaJson = JsonSerializer.Serialize(meta, JsonOptions);
            await File.WriteAllTextAsync(Path.Combine(themeDir, "meta.json"), metaJson, ct);

            return new DownloadResult("Yaru", icons.Count, skipped);
        }
        finally
        {
            if (File.Exists(tempZip))
                File.Delete(tempZip);
        }
    }

    private static void RenderSvgToPng(string svgPath, string pngPath)
    {
        using var svg = new SKSvg();
        svg.Load(svgPath);

        if (svg.Picture is null)
            throw new InvalidOperationException("Failed to load SVG");

        var info = new SKImageInfo(RenderSize, RenderSize);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var bounds = svg.Picture.CullRect;
        float scale = Math.Min((float)RenderSize / bounds.Width, (float)RenderSize / bounds.Height);
        float tx = (RenderSize - bounds.Width * scale) / 2f;
        float ty = (RenderSize - bounds.Height * scale) / 2f;
        canvas.Translate(tx, ty);
        canvas.Scale(scale);
        canvas.DrawPicture(svg.Picture);
        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var fileStream = File.OpenWrite(pngPath);
        data.SaveTo(fileStream);
    }
}
