using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WinNotifier.Interfaces;
using WinNotifier.Models;

namespace WinNotifier.Services;

public class NotificationApiServer : IHttpServerService
{
    private readonly INotificationService _notificationService;
    private readonly IEmojiResolver _emojiResolver;
    private readonly int _port;
    private readonly string? _token;
    private WebApplication? _app;
    private string? _resolvedUrl;

    public string ListenUrl => _resolvedUrl ?? $"http://0.0.0.0:{_port}";

    public NotificationApiServer(INotificationService notificationService, IEmojiResolver emojiResolver, int port = 8080, string? token = null)
    {
        _notificationService = notificationService;
        _emojiResolver = emojiResolver;
        _port = port;
        _token = token;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{_port}");
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        _app = builder.Build();

        if (!string.IsNullOrEmpty(_token))
        {
            _app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/notify"))
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (!ValidateBasicAuth(authHeader, _token))
                    {
                        context.Response.StatusCode = 401;
                        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"WinNotifier\"";
                        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                        return;
                    }
                }
                await next();
            });
        }

        _app.MapGet("/icons", (HttpRequest httpRequest) =>
        {
            var icons = _emojiResolver.GetAll();
            var accept = httpRequest.Headers.Accept.ToString();

            if (accept.Contains("text/plain"))
            {
                var sb = new StringBuilder();
                foreach (var group in icons.GroupBy(i => i.Category))
                {
                    sb.AppendLine();
                    sb.AppendLine($"  {group.Key}");
                    sb.AppendLine($"  {new string('-', group.Key.Length)}");
                    foreach (var icon in group)
                        sb.AppendLine($"    {icon.Emoji}  {string.Join(", ", icon.Aliases)}");
                }
                return Results.Text(sb.ToString(), "text/plain; charset=utf-8");
            }

            var grouped = icons
                .GroupBy(i => i.Category)
                .Select(g => new { category = g.Key, icons = g.Select(i => new { i.Emoji, i.Aliases }) });
            return Results.Ok(grouped);
        });

        _app.MapPost("/notify", async (HttpRequest httpRequest) =>
        {
            var request = await ParseRequestAsync(httpRequest);
            if (request is null)
                return Results.BadRequest(new { errors = new[] { "Invalid request body." } });

            var errors = request.Validate();
            if (errors.Count > 0)
                return Results.BadRequest(new { errors });

            await _notificationService.ShowAsync(request);
            return Results.Ok(new { status = "sent" });
        })
        .Accepts<NotificationRequest>("application/json", "application/x-www-form-urlencoded");

        await _app.StartAsync(cancellationToken);

        var addresses = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>();
        if (addresses?.Addresses.Count > 0)
            _resolvedUrl = addresses.Addresses.First();
    }

    private static async Task<NotificationRequest?> ParseRequestAsync(HttpRequest httpRequest)
    {
        var contentType = httpRequest.ContentType ?? "";

        if (contentType.Contains("application/json"))
        {
            return await httpRequest.ReadFromJsonAsync<NotificationRequest>();
        }

        if (contentType.Contains("application/x-www-form-urlencoded"))
        {
            var encoding = GetEncodingFromContentType(contentType);
            using var reader = new StreamReader(httpRequest.Body, encoding);
            var body = await reader.ReadToEndAsync();
            var fields = ParseFormBody(body);
            return new NotificationRequest
            {
                Title = fields.GetValueOrDefault("title"),
                Message = fields.GetValueOrDefault("message"),
                From = fields.GetValueOrDefault("from"),
                Icon = fields.GetValueOrDefault("icon")
            };
        }

        return null;
    }

    private static Encoding GetEncodingFromContentType(string contentType)
    {
        try
        {
            var mediaType = MediaTypeHeaderValue.Parse(contentType);
            if (!string.IsNullOrEmpty(mediaType.CharSet))
                return Encoding.GetEncoding(mediaType.CharSet);
        }
        catch
        {
            // ignore parse errors, fall through to default
        }
        return Encoding.UTF8;
    }

    private static Dictionary<string, string> ParseFormBody(string body)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var index = pair.IndexOf('=');
            if (index < 0) continue;
            var key = pair[..index];
            var value = pair[(index + 1)..];
            result[key] = value;
        }
        return result;
    }

    private static bool ValidateBasicAuth(string authHeader, string token)
    {
        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader[6..]));
            var colonIndex = credentials.IndexOf(':');
            if (colonIndex < 0)
                return false;

            var password = credentials[(colonIndex + 1)..];
            return password == token;
        }
        catch
        {
            return false;
        }
    }

    public async Task StopAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }
    }
}
