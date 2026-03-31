using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var (title, message, icon, from) = ParseArgs(args);

if (title is null || message is null)
{
    Console.Error.WriteLine("Usage: Notify -t <title> -m <message> [-i <icon>] [-f <from>]");
    return 1;
}

var config = Config.Load();
var url = $"http://localhost:{config.Port}/notify";

var payload = new Dictionary<string, string?>
{
    ["title"] = title,
    ["message"] = message,
    ["icon"] = icon,
    ["from"] = from
};

// Remove null entries
var json = JsonSerializer.Serialize(
    payload.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value));

using var client = new HttpClient();
if (!string.IsNullOrEmpty(config.Token))
{
    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{config.Token}"));
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", credentials);
}
var content = new StringContent(json, Encoding.UTF8, "application/json");

try
{
    var response = await client.PostAsync(url, content);
    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        Console.Error.WriteLine($"Error: {response.StatusCode} {body}");
        return 1;
    }
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Error: Could not connect to WinNotifier at {url}");
    Console.Error.WriteLine(ex.Message);
    return 1;
}

return 0;

static (string? title, string? message, string? icon, string? from) ParseArgs(string[] args)
{
    string? title = null, message = null, icon = null, from = null;

    for (int i = 0; i < args.Length; i++)
    {
        var next = i + 1 < args.Length ? args[i + 1] : null;
        switch (args[i])
        {
            case "-t" or "--title":
                title = next;
                i++;
                break;
            case "-m" or "--message":
                message = next;
                i++;
                break;
            case "-i" or "--icon":
                icon = next;
                i++;
                break;
            case "-f" or "--from":
                from = next;
                i++;
                break;
        }
    }

    return (title, message, icon, from);
}

sealed record Config(
    [property: JsonPropertyName("port")] int Port = 8080,
    [property: JsonPropertyName("token")] string? Token = null
)
{
    public static Config Load()
    {
        string exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        string configPath = Path.Combine(exeDir, "config.json");

        if (!File.Exists(configPath))
            return new Config();

        string json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<Config>(json) ?? new Config();
    }
}
