using IconDownloader.Models;

namespace IconDownloader.Downloaders;

public interface IIconSetDownloader
{
    string Name { get; }
    string Category { get; } // "theme" or "mapping"
    string NewVersion { get; }
    string ExistingMetaPath(string outputDir);
    Task<DownloadResult> DownloadAsync(
        string outputDir,
        IProgress<DownloadProgress> progress,
        CancellationToken ct);
}

public sealed record DownloadResult(
    string ThemeName,
    int IconCount,
    int SkippedCount
);
