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

        var iconsDir = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ?? ".", "icons");
        IIconResolver iconResolver = new IconResolver(iconsDir, config.Icons?.Mappings, config.Icons?.Themes);
        INotificationService notifier = new ToastNotificationService(iconResolver);
        IHttpServerService server = new NotificationApiServer(notifier, iconResolver, port: config.Port, token: config.Token);

        Application.Run(new TrayApplicationContext(server, config.Token));
    }
}
