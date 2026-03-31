using System.Text;
using WinNotifier;
using WinNotifier.Interfaces;
using WinNotifier.Services;

namespace WinNotifier;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ApplicationConfiguration.Initialize();

        Config.Init();
        var config = Config.Load();

        var httpClient = new HttpClient();
        IEmojiResolver emojiResolver = new EmojiResolver(httpClient);
        INotificationService notifier = new ToastNotificationService(emojiResolver);
        IHttpServerService server = new NotificationApiServer(notifier, port: config.Port, token: config.Token);

        Application.Run(new TrayApplicationContext(server, config.Token));
    }
}
