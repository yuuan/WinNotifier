using CommunityToolkit.WinUI.Notifications;
using WinNotifier.Interfaces;
using WinNotifier.Models;

namespace WinNotifier.Services;

public class ToastNotificationService : INotificationService
{
    private readonly IIconResolver _iconResolver;

    public ToastNotificationService(IIconResolver iconResolver)
    {
        _iconResolver = iconResolver;
    }

    public async Task ShowAsync(NotificationRequest request)
    {
        var builder = new ToastContentBuilder()
            .AddText(request.Title)
            .AddText(request.Message);

        if (!string.IsNullOrWhiteSpace(request.From))
            builder.AddAttributionText(request.From);

        if (!string.IsNullOrWhiteSpace(request.Icon))
        {
            var iconPath = await _iconResolver.ResolveAsync(request.Icon);
            if (iconPath is not null)
            {
                builder.AddAppLogoOverride(
                    new Uri($"file:///{iconPath.Replace('\\', '/')}"),
                    ToastGenericAppLogoCrop.None);
            }
        }

        builder.Show();
    }
}
