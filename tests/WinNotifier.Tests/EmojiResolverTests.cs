using System.Net;
using System.Text;
using Xunit;
using WinNotifier.Services;

namespace WinNotifier.Tests;

public class EmojiResolverTests : IDisposable
{
    private readonly string _cacheDir;
    private readonly HttpClient _httpClient;
    private readonly EmojiResolver _resolver;

    public EmojiResolverTests()
    {
        _cacheDir = Path.Combine(Path.GetTempPath(), "winnotifier-test-" + Guid.NewGuid().ToString("N"));
        _httpClient = new HttpClient(new FakeCdnHandler());
        _resolver = new EmojiResolver(_httpClient, _cacheDir);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        if (Directory.Exists(_cacheDir))
            Directory.Delete(_cacheDir, true);
    }

    [Fact]
    public async Task Resolve_Shortcode_ReturnsPathWithCorrectFilename()
    {
        var path = await _resolver.ResolveAsync("rocket");

        Assert.NotNull(path);
        Assert.EndsWith("1f680.png", path);
    }

    [Fact]
    public async Task Resolve_ColonWrappedShortcode_ReturnsPathWithCorrectFilename()
    {
        var path = await _resolver.ResolveAsync(":rocket:");

        Assert.NotNull(path);
        Assert.EndsWith("1f680.png", path);
    }

    [Fact]
    public async Task Resolve_UnicodeEmoji_ReturnsPathWithCorrectFilename()
    {
        var path = await _resolver.ResolveAsync("🚀");

        Assert.NotNull(path);
        Assert.EndsWith("1f680.png", path);
    }

    [Fact]
    public async Task Resolve_UnknownShortcode_ReturnsNull()
    {
        var path = await _resolver.ResolveAsync("not_a_real_emoji_xyz");

        Assert.Null(path);
    }

    [Fact]
    public async Task Resolve_EmptyInput_ReturnsNull()
    {
        var path = await _resolver.ResolveAsync("");

        Assert.Null(path);
    }

    [Fact]
    public async Task Resolve_CachedFile_DoesNotDownloadAgain()
    {
        var handler = new CountingCdnHandler();
        var httpClient = new HttpClient(handler);
        var resolver = new EmojiResolver(httpClient, _cacheDir);

        await resolver.ResolveAsync("rocket");
        await resolver.ResolveAsync("rocket");

        Assert.Equal(1, handler.RequestCount);

        httpClient.Dispose();
    }

    [Fact]
    public void UnicodeToFilename_SkipsVariationSelector()
    {
        // ❤️ = U+2764 U+FE0F — FE0F should be stripped
        var filename = EmojiResolver.UnicodeToFilename("\u2764\uFE0F");

        Assert.Equal("2764.png", filename);
    }

    private class FakeCdnHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 }) // PNG header stub
            };
            return Task.FromResult(response);
        }
    }

    private class CountingCdnHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 })
            };
            return Task.FromResult(response);
        }
    }
}
