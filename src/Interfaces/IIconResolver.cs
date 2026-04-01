namespace WinNotifier.Interfaces;

public interface IIconResolver
{
    Task<string?> ResolveAsync(string iconInput, CancellationToken ct = default);
    IReadOnlyList<IconInfo> GetAll();
}

public record IconInfo(string Name, string FilePath, IReadOnlyList<string> Aliases, string Theme);
