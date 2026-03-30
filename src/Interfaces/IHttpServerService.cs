namespace WinNotifier.Interfaces;

public interface IHttpServerService
{
    string ListenUrl { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}
