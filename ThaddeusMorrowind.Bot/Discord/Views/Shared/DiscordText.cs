namespace ThaddeusMorrowind.Bot.Discord.Views.Shared;

public static class DiscordText
{
    public static string FormatStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "active" => "Activa",
            "blocked" => "Bloqueada",
            "deleted" => "Eliminada",
            _ => status
        };
    }

    public static string FormatUtc(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm 'UTC'");
    }

    public static string SafeValue(string? value, string fallback = "No definido")
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
