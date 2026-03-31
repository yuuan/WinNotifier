namespace WinNotifier.Interfaces;

public interface IEmojiResolver
{
    Task<string?> ResolveAsync(string iconInput, CancellationToken ct = default);
    IReadOnlyList<EmojiIcon> GetAll();
}

public record EmojiIcon(string Emoji, string Category, IReadOnlyList<string> Aliases);
