using System.Net.Sockets;
using System.Reflection;
using WinNotifier.Interfaces;

namespace WinNotifier;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly IHttpServerService _server;
    private readonly string? _token;
    private readonly CancellationTokenSource _cts = new();

    public TrayApplicationContext(IHttpServerService server, string? token)
    {
        _server = server;
        _token = token;

        _trayIcon = new NotifyIcon
        {
            Icon = LoadEmbeddedIcon(),
            Text = $"WinNotifier - {server.ListenUrl}/notify",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _ = StartServerAsync();
    }

    private async Task StartServerAsync()
    {
        try
        {
            await _server.StartAsync(_cts.Token);
        }
        catch (Exception ex) when (ex is IOException or SocketException
            || ex.InnerException is IOException or SocketException)
        {
            var configPath = Config.GetPath();
            MessageBox.Show(
                $"ポート {_server.ListenUrl} は既に使用されています。\n"
                + $"ポートを開放するか、{configPath} でポート番号を変更してください。",
                "WinNotifier", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start HTTP server:\n{ex.Message}",
                "WinNotifier", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Exit();
        }
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        var copyItem = menu.Items.Add("トークンをコピー (&C)", null, OnCopyToken);
        copyItem.Enabled = !string.IsNullOrEmpty(_token);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了 (&X)", null, OnExit);
        return menu;
    }

    private void OnCopyToken(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_token))
            Clipboard.SetText(_token);
    }

    private async void OnExit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _cts.Cancel();
        await _server.StopAsync();
        Application.Exit();
    }

    private static Icon LoadEmbeddedIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("WinNotifier.icon.ico")
            ?? throw new FileNotFoundException("Embedded icon resource not found.");
        return new Icon(stream);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _cts.Dispose();
        }
        base.Dispose(disposing);
    }
}
