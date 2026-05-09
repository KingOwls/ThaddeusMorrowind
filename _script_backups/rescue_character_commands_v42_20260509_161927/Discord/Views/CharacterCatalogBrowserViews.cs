using Discord;
using System.Text;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class CharacterCatalogBrowserViews
{
    public static Embed Detail(
        string title,
        string intro,
        CharacterCatalogOptionDto option,
        int index,
        int total)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"{title} · {option.Name}")
            .WithDescription(BuildDescription(intro, option, index, total))
            .WithColor(Color.Purple)
            .AddField("Key para crear personaje", $"`{option.Key}`", inline: true)
            .AddField("Posición", $"{index + 1}/{total}", inline: true)
            .WithFooter("Usa ◀ y ▶ para navegar. El panel se actualiza sin crear mensajes nuevos.")
            .WithCurrentTimestamp();

        string? iconUrl = NormalizeImageUrl(option.IconUrl);
        string? bannerUrl = NormalizeImageUrl(option.BannerUrl);

        if (!string.IsNullOrWhiteSpace(iconUrl))
        {
            builder.WithThumbnailUrl(iconUrl);
            builder.WithAuthor(option.Name, iconUrl);
        }
        else
        {
            builder.WithAuthor(option.Name);
        }

        if (!string.IsNullOrWhiteSpace(bannerUrl))
        {
            builder.WithImageUrl(bannerUrl);
        }
        else if (!string.IsNullOrWhiteSpace(iconUrl))
        {
            builder.WithImageUrl(iconUrl);
        }

        return builder.Build();
    }

    public static Embed Empty(string catalogType)
    {
        return new EmbedBuilder()
            .WithTitle("📭 Catálogo vacío")
            .WithDescription($"No encontré opciones activas para `{catalogType}`.")
            .WithColor(Color.Orange)
            .WithFooter("Thaddeus Morrowind · Catálogo")
            .WithCurrentTimestamp()
            .Build();
    }

    private static string BuildDescription(
        string intro,
        CharacterCatalogOptionDto option,
        int index,
        int total)
    {
        StringBuilder builder = new();

        builder.AppendLine(intro);
        builder.AppendLine();
        builder.AppendLine($"`{index + 1}/{total}` **{option.Name}**");
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(option.ShortDescription))
        {
            builder.AppendLine($"**{option.ShortDescription}**");
            builder.AppendLine();
        }

        builder.AppendLine(
            string.IsNullOrWhiteSpace(option.Description)
                ? "Descripción extendida pendiente."
                : option.Description);

        return builder.ToString();
    }

    private static string? NormalizeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return url.Replace("/svg?", "/png?", StringComparison.OrdinalIgnoreCase);
    }
}
