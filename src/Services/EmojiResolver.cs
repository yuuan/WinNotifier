using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WinNotifier.Interfaces;

namespace WinNotifier.Services;

public class EmojiResolver : IEmojiResolver
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;
    private readonly Lazy<Dictionary<string, string>> _shortcodeToUnicode;

    private const string CdnBaseUrl = "https://cdn.jsdelivr.net/gh/jdecked/twemoji@latest/assets/72x72/";

    public EmojiResolver(HttpClient httpClient, string? cacheDirectory = null)
    {
        _httpClient = httpClient;
        _cacheDirectory = cacheDirectory
            ?? Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? ".", "emoji-cache");
        _shortcodeToUnicode = new Lazy<Dictionary<string, string>>(LoadShortcodeMap);
    }

    public async Task<string?> ResolveAsync(string iconInput, CancellationToken ct = default)
    {
        var input = iconInput.Trim().Trim(':');
        if (string.IsNullOrEmpty(input))
            return null;

        var unicode = ResolveToUnicode(input);
        if (unicode is null)
            return null;

        var filename = UnicodeToFilename(unicode);
        if (filename is null)
            return null;

        var cachedPath = Path.Combine(_cacheDirectory, filename);
        if (File.Exists(cachedPath))
            return cachedPath;

        return await DownloadAsync(filename, cachedPath, ct);
    }

    private string? ResolveToUnicode(string input)
    {
        if (_shortcodeToUnicode.Value.TryGetValue(input, out var unicode))
            return unicode;

        if (ContainsEmoji(input))
            return input;

        return null;
    }

    private static bool ContainsEmoji(string input)
    {
        var enumerator = StringInfo.GetTextElementEnumerator(input);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();
            foreach (var rune in element.EnumerateRunes())
            {
                if (rune.Value > 0x7F)
                    return true;
            }
        }
        return false;
    }

    internal static string? UnicodeToFilename(string unicode)
    {
        var codepoints = new List<string>();
        foreach (var rune in unicode.EnumerateRunes())
        {
            if (rune.Value == 0xFE0F) // skip variation selector
                continue;
            codepoints.Add(rune.Value.ToString("x"));
        }

        if (codepoints.Count == 0)
            return null;

        return string.Join("-", codepoints) + ".png";
    }

    private async Task<string?> DownloadAsync(string filename, string cachedPath, CancellationToken ct)
    {
        try
        {
            var url = CdnBaseUrl + filename;
            var response = await _httpClient.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            Directory.CreateDirectory(_cacheDirectory);
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            await File.WriteAllBytesAsync(cachedPath, bytes, ct);
            return cachedPath;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, string> LoadShortcodeMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("WinNotifier.emoji.json");
        if (stream is null)
            return map;

        var entries = JsonSerializer.Deserialize<List<EmojiEntry>>(stream) ?? [];
        foreach (var entry in entries)
        {
            foreach (var alias in entry.Aliases)
            {
                map.TryAdd(alias, entry.Emoji);
            }
        }

        return map;
    }

    private sealed record EmojiEntry(
        [property: JsonPropertyName("emoji")] string Emoji,
        [property: JsonPropertyName("aliases")] List<string> Aliases
    );
}
