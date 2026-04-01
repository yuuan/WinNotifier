using System.Text.Json;
using System.Text.Json.Serialization;

namespace IconDownloader.Models;

public sealed record ThemeMeta
{
    [JsonPropertyName("theme")]
    public required string Theme { get; init; }

    [JsonPropertyName("source")]
    public required SourceInfo Source { get; init; }

    [JsonPropertyName("downloadedAt")]
    [JsonConverter(typeof(DateTimeOffsetSecondsConverter))]
    public required DateTimeOffset DownloadedAt { get; init; }

    [JsonPropertyName("icons")]
    public required List<IconEntry> Icons { get; init; }
}

public sealed record SourceInfo
{
    [JsonPropertyName("repository")]
    public required string Repository { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

public sealed record IconEntry
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("file")]
    public required string File { get; init; }

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; init; } = [];
}

internal class DateTimeOffsetSecondsConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-ddTHH:mm:sszzz";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(Format));
}
