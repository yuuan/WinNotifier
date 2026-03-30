using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinNotifier;

internal sealed record Config(
    [property: JsonPropertyName("port")] int Port = 8080
)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string GetPath()
    {
        string exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        return Path.Combine(exeDir, "config.json");
    }

    public static Config Load()
    {
        string configPath = GetPath();

        if (!File.Exists(configPath))
        {
            return new Config();
        }

        string json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<Config>(json) ?? new Config();
    }

    public static (bool created, string path) Init()
    {
        string configPath = GetPath();

        if (File.Exists(configPath))
        {
            return (false, configPath);
        }

        string json = JsonSerializer.Serialize(new Config(), JsonOptions);
        File.WriteAllText(configPath, json);
        return (true, configPath);
    }
}
