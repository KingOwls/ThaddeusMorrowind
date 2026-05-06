using System.Text.Json;

namespace ThaddeusMorrowind.Bot.Common.Helpers;

public static class JsonPreview
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static string ToJsonBlock<T>(T value, int maxLength = 900)
    {
        string json = JsonSerializer.Serialize(value, Options);

        if (json.Length > maxLength)
        {
            json = json[..maxLength] + "\n...";
        }

        return $"```json\n{json}\n```";
    }
}
