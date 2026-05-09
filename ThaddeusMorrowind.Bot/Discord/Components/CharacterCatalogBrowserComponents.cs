using Discord;

namespace ThaddeusMorrowind.Bot.Discord.Components;

public static class CharacterCatalogBrowserComponents
{
    public static MessageComponent BrowserButtons(
        string catalogType,
        int currentIndex,
        int total)
    {
        return new ComponentBuilder()
            .WithButton("◀", $"character:catalogbrowse:{catalogType}:prev:{currentIndex}", ButtonStyle.Secondary, disabled: total <= 1)
            .WithButton("▶", $"character:catalogbrowse:{catalogType}:next:{currentIndex}", ButtonStyle.Secondary, disabled: total <= 1)
            .Build();
    }
}
