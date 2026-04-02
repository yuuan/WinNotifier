using System.Text.Json;
using System.Text.Json.Serialization;
using WinNotifier.Interfaces;

namespace WinNotifier.Services;

public class IconResolver : IIconResolver
{
    private readonly Dictionary<string, string> _mappings;
    private readonly Dictionary<string, string> _iconIndex;
    private readonly List<IconInfo> _allIcons;

    public IconResolver(string iconsDir, IReadOnlyList<string>? mappingOrder = null, IReadOnlyList<string>? themeOrder = null)
    {
        _mappings = LoadMappings(Path.Combine(iconsDir, "mappings"), mappingOrder);
        (_iconIndex, _allIcons) = LoadThemes(Path.Combine(iconsDir, "themes"), themeOrder);
    }

    public Task<string?> ResolveAsync(string iconInput, CancellationToken ct = default)
    {
        var input = iconInput.Trim().Trim(':');
        if (string.IsNullOrEmpty(input))
            return Task.FromResult<string?>(null);

        // Apply mappings (e.g. "rocket" → "🚀")
        if (_mappings.TryGetValue(input, out var mapped))
            input = mapped;

        // Look up in themes by name or alias
        var normalized = StripVariationSelectors(input);
        if (_iconIndex.TryGetValue(normalized, out var path))
            return Task.FromResult<string?>(path);

        // Try original input (before mapping) without variation selectors
        if (_iconIndex.TryGetValue(input, out path))
            return Task.FromResult<string?>(path);

        return Task.FromResult<string?>(null);
    }

    private static string StripVariationSelectors(string input)
    {
        return input.Replace("\uFE0F", "");
    }

    public IReadOnlyList<IconInfo> GetAll() => _allIcons;

    private static Dictionary<string, string> LoadMappings(string mappingsDir, IReadOnlyList<string>? order)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(mappingsDir))
            return result;

        var files = OrderFiles(Directory.GetFiles(mappingsDir, "*.json"), order);

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var mapping = JsonSerializer.Deserialize<MappingFile>(json);
                if (mapping?.Mappings is null) continue;

                foreach (var (key, value) in mapping.Mappings)
                    result.TryAdd(key, value);
            }
            catch
            {
                // skip malformed files
            }
        }

        return result;
    }

    private static (Dictionary<string, string> index, List<IconInfo> all) LoadThemes(string themesDir, IReadOnlyList<string>? order)
    {
        var index = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var all = new List<IconInfo>();

        if (!Directory.Exists(themesDir))
            return (index, all);

        var dirs = OrderDirectories(Directory.GetDirectories(themesDir), order);

        foreach (var themeDir in dirs)
        {
            var metaPath = Path.Combine(themeDir, "meta.json");
            if (!File.Exists(metaPath)) continue;

            try
            {
                var json = File.ReadAllText(metaPath);
                var meta = JsonSerializer.Deserialize<ThemeFile>(json);
                if (meta?.Icons is null) continue;

                var themeName = meta.Theme ?? Path.GetFileName(themeDir);

                foreach (var icon in meta.Icons)
                {
                    if (icon.Name is null || icon.File is null) continue;

                    var fullPath = Path.Combine(themeDir, icon.File);
                    if (!File.Exists(fullPath)) continue;

                    var aliases = icon.Aliases ?? [];
                    all.Add(new IconInfo(icon.Name, fullPath, aliases, themeName));

                    index.TryAdd(icon.Name, fullPath);
                    index.TryAdd(StripVariationSelectors(icon.Name), fullPath);
                    foreach (var alias in aliases)
                    {
                        index.TryAdd(alias, fullPath);
                        index.TryAdd(StripVariationSelectors(alias), fullPath);
                    }
                }
            }
            catch
            {
                // skip malformed files
            }
        }

        return (index, all);
    }

    private static IEnumerable<string> OrderFiles(string[] files, IReadOnlyList<string>? order)
    {
        if (order is null || order.Count == 0)
            return files;

        var byName = files.ToDictionary(f => Path.GetFileNameWithoutExtension(f), f => f, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();

        foreach (var name in order)
        {
            if (byName.Remove(name, out var file))
                ordered.Add(file);
        }

        ordered.AddRange(byName.Values);
        return ordered;
    }

    private static IEnumerable<string> OrderDirectories(string[] dirs, IReadOnlyList<string>? order)
    {
        if (order is null || order.Count == 0)
            return dirs;

        var byName = dirs.ToDictionary(d => Path.GetFileName(d), d => d, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();

        foreach (var name in order)
        {
            if (byName.Remove(name, out var dir))
                ordered.Add(dir);
        }

        ordered.AddRange(byName.Values);
        return ordered;
    }

    private sealed record MappingFile(
        [property: JsonPropertyName("mappings")] Dictionary<string, string>? Mappings
    );

    private sealed record ThemeFile(
        [property: JsonPropertyName("theme")] string? Theme,
        [property: JsonPropertyName("icons")] List<ThemeIcon>? Icons
    );

    private sealed record ThemeIcon(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("file")] string? File,
        [property: JsonPropertyName("aliases")] List<string>? Aliases
    );
}
