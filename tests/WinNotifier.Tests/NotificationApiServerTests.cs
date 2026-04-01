using System.Net;
using System.Text;
using NSubstitute;
using Xunit;
using WinNotifier.Interfaces;
using WinNotifier.Models;
using WinNotifier.Services;

namespace WinNotifier.Tests;

public class NotificationApiServerTests : IAsyncLifetime
{
    private readonly INotificationService _mockNotifier = Substitute.For<INotificationService>();
    private NotificationApiServer _server = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _server = new NotificationApiServer(_mockNotifier, Substitute.For<IIconResolver>(), port: 0);
        await _server.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_server.ListenUrl.Replace("0.0.0.0", "localhost")) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _server.StopAsync();
    }

    [Fact]
    public async Task PostNotify_ValidPayload_Returns200AndCallsService()
    {
        var json = """{"title":"Test","message":"Hello"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _mockNotifier.Received(1).ShowAsync(
            Arg.Is<NotificationRequest>(r => r.Title == "Test" && r.Message == "Hello"));
    }

    [Fact]
    public async Task PostNotify_MissingTitle_Returns400()
    {
        var json = """{"message":"Hello"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _mockNotifier.DidNotReceiveWithAnyArgs().ShowAsync(default!);
    }

    [Fact]
    public async Task PostNotify_MissingMessage_Returns400()
    {
        var json = """{"title":"Test"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostNotify_EmptyBody_Returns400()
    {
        var json = """{}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostNotify_FormData_Returns200AndCallsService()
    {
        var body = new StringContent("title=FormTest&message=Hello", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await _client.PostAsync("/notify", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _mockNotifier.Received(1).ShowAsync(
            Arg.Is<NotificationRequest>(r => r.Title == "FormTest" && r.Message == "Hello"));
    }

    [Fact]
    public async Task PostNotify_FormData_MissingFields_Returns400()
    {
        var body = new StringContent("title=OnlyTitle", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await _client.PostAsync("/notify", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostNotify_UnsupportedContentType_Returns400()
    {
        var content = new StringContent("plain text", Encoding.UTF8, "text/plain");

        var response = await _client.PostAsync("/notify", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task PostNotify_InvalidRoute_Returns404()
    {
        var json = """{"title":"Test","message":"Hello"}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/invalid", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
