using System.Text.Encodings.Web;
using System.Text.Json;
using Xunit;
using WinNotifier.Services;

namespace WinNotifier.Tests;

public class IconResolverTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IconResolver _resolver;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public IconResolverTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "winnotifier-test-" + Guid.NewGuid().ToString("N"));
        SetupTestIcons();
        _resolver = new IconResolver(Path.Combine(_tempDir, "icons"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void SetupTestIcons()
    {
        var mappingsDir = Path.Combine(_tempDir, "icons", "mappings");
        var themeDir = Path.Combine(_tempDir, "icons", "themes", "TestTheme");
        var assetsDir = Path.Combine(themeDir, "assets");
        Directory.CreateDirectory(mappingsDir);
        Directory.CreateDirectory(assetsDir);

        // Create mapping file
        var mapping = new { name = "test", source = new { repository = "test", version = "v1" }, mappings = new Dictionary<string, string> { ["rocket"] = "🚀", ["star"] = "⭐" } };
        File.WriteAllText(Path.Combine(mappingsDir, "test.json"), JsonSerializer.Serialize(mapping, JsonOptions));

        // Create a dummy PNG
        File.WriteAllBytes(Path.Combine(assetsDir, "1f680.png"), [0x89, 0x50, 0x4E, 0x47]);
        File.WriteAllBytes(Path.Combine(assetsDir, "dialog-warning.png"), [0x89, 0x50, 0x4E, 0x47]);

        // Create meta.json
        var meta = new
        {
            theme = "TestTheme",
            source = new { repository = "test", version = "v1" },
            downloadedAt = "2026-01-01T00:00:00+09:00",
            icons = new[]
            {
                new { name = "1f680", file = "assets\\1f680.png", aliases = new[] { "🚀" } },
                new { name = "dialog-warning", file = "assets\\dialog-warning.png", aliases = new[] { "warning-icon" } }
            }
        };
        File.WriteAllText(Path.Combine(themeDir, "meta.json"), JsonSerializer.Serialize(meta, JsonOptions));
    }

    [Fact]
    public async Task Resolve_MappedShortcode_ReturnsPath()
    {
        var path = await _resolver.ResolveAsync("rocket");
        Assert.NotNull(path);
        Assert.EndsWith("1f680.png", path);
    }

    [Fact]
    public async Task Resolve_ColonWrappedShortcode_ReturnsPath()
    {
        var path = await _resolver.ResolveAsync(":rocket:");
        Assert.NotNull(path);
        Assert.EndsWith("1f680.png", path);
    }

    [Fact]
    public async Task Resolve_DirectEmoji_ReturnsPath()
    {
        var path = await _resolver.ResolveAsync("🚀");
        Assert.NotNull(path);
        Assert.EndsWith("1f680.png", path);
    }

    [Fact]
    public async Task Resolve_ThemeNameDirectly_ReturnsPath()
    {
        var path = await _resolver.ResolveAsync("dialog-warning");
        Assert.NotNull(path);
        Assert.EndsWith("dialog-warning.png", path);
    }

    [Fact]
    public async Task Resolve_ThemeAlias_ReturnsPath()
    {
        var path = await _resolver.ResolveAsync("warning-icon");
        Assert.NotNull(path);
        Assert.EndsWith("dialog-warning.png", path);
    }

    [Fact]
    public async Task Resolve_UnknownName_ReturnsNull()
    {
        var path = await _resolver.ResolveAsync("not_a_real_icon");
        Assert.Null(path);
    }

    [Fact]
    public async Task Resolve_EmptyInput_ReturnsNull()
    {
        var path = await _resolver.ResolveAsync("");
        Assert.Null(path);
    }

    [Fact]
    public void GetAll_ReturnsAllIcons()
    {
        var all = _resolver.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetAll_EmptyDir_ReturnsEmpty()
    {
        var emptyDir = Path.Combine(_tempDir, "empty-icons");
        var resolver = new IconResolver(emptyDir);
        Assert.Empty(resolver.GetAll());
    }
}
