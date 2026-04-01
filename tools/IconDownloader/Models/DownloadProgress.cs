namespace IconDownloader.Models;

public sealed record DownloadProgress(
    string Status,
    string Detail,
    int Current,
    int Total
);
