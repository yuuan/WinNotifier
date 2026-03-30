using WinNotifier.Models;

namespace WinNotifier.Interfaces;

public interface INotificationService
{
    Task ShowAsync(NotificationRequest request);
}
