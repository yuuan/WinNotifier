using CommunityToolkit.WinUI.Notifications;
using WinNotifier.Interfaces;
using WinNotifier.Models;

namespace WinNotifier.Services;

public class ToastNotificationService : INotificationService
{
    private readonly IEmojiResolver _emojiResolver;

    public ToastNotificationService(IEmojiResolver emojiResolver)
    {
        _emojiResolver = emojiResolver;
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
            var iconPath = await _emojiResolver.ResolveAsync(request.Icon);
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
