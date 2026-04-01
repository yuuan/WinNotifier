using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconDownloader.Models;

public sealed record MappingMeta
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("source")]
    public required SourceInfo Source { get; init; }

    [JsonPropertyName("downloadedAt")]
    [JsonConverter(typeof(DateTimeOffsetSecondsConverter))]
    public required DateTimeOffset DownloadedAt { get; init; }

    [JsonPropertyName("mappings")]
    public required Dictionary<string, string> Mappings { get; init; }
}
