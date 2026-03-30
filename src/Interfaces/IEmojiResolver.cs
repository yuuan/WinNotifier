namespace WinNotifier.Interfaces;

public interface IEmojiResolver
{
    Task<string?> ResolveAsync(string iconInput, CancellationToken ct = default);
}
