using System.Net;
using System.Net.Http.Headers;
using System.Text;
using NSubstitute;
using Xunit;
using WinNotifier.Interfaces;
using WinNotifier.Services;

namespace WinNotifier.Tests;

public class NotificationApiServerAuthTests : IAsyncLifetime
{
    private readonly INotificationService _mockNotifier = Substitute.For<INotificationService>();
    private NotificationApiServer _server = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _server = new NotificationApiServer(_mockNotifier, Substitute.For<IEmojiResolver>(), port: 0, token: "secret123");
        await _server.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_server.ListenUrl.Replace("0.0.0.0", "localhost")) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _server.StopAsync();
    }

    [Fact]
    public async Task PostNotify_ValidToken_Returns200()
    {
        var json = """{"title":"Test","message":"Hello"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret123")));

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostNotify_WrongToken_Returns401()
    {
        var json = """{"title":"Test","message":"Hello"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("user:wrongtoken")));

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostNotify_NoAuth_Returns401()
    {
        var json = """{"title":"Test","message":"Hello"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostNotify_EmptyUsername_ValidToken_Returns200()
    {
        var json = """{"title":"Test","message":"Hello"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(":secret123")));

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
