using Discord;
using System.Text;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class CharacterCatalogViews
{
    public static Embed List(string title, string description, IReadOnlyList<CharacterCatalogOptionDto> options)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(BuildListDescription(description, options))
            .WithColor(Color.Blue)
            .WithFooter("Thaddeus Morrowind · Catálogo de personaje")
            .WithCurrentTimestamp();

        CharacterCatalogOptionDto? firstWithIcon = options.FirstOrDefault(option => !string.IsNullOrWhiteSpace(option.IconUrl));
        CharacterCatalogOptionDto? firstWithBanner = options.FirstOrDefault(option => !string.IsNullOrWhiteSpace(option.BannerUrl));

        if (firstWithIcon is not null)
        {
            builder.WithThumbnailUrl(firstWithIcon.IconUrl);
        }

        if (firstWithBanner is not null)
        {
            builder.WithImageUrl(firstWithBanner.BannerUrl);
        }

        return builder.Build();
    }

    public static Embed Detail(CharacterCatalogOptionDto option)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"{CatalogEmoji(option.CatalogType)} {option.Name}")
            .WithDescription(BuildDetailDescription(option))
            .WithColor(Color.Purple)
            .AddField("Key para comandos", $"`{option.Key}`", inline: true)
            .AddField("Orden", option.DisplayOrder.ToString(), inline: true)
            .WithFooter("Usa esta key al crear personajes.")
            .WithCurrentTimestamp();

        if (!string.IsNullOrWhiteSpace(option.IconUrl))
        {
            builder.WithThumbnailUrl(option.IconUrl);
        }

        if (!string.IsNullOrWhiteSpace(option.BannerUrl))
        {
            builder.WithImageUrl(option.BannerUrl);
        }

        return builder.Build();
    }

    public static Embed Missing(string catalogType)
    {
        return new EmbedBuilder()
            .WithTitle("📭 Catálogo vacío")
            .WithDescription($"No encontré opciones activas para `{catalogType}`.")
            .WithColor(Color.Orange)
            .WithFooter("Thaddeus Morrowind · Catálogo")
            .WithCurrentTimestamp()
            .Build();
    }

    private static string BuildListDescription(
        string description,
        IReadOnlyList<CharacterCatalogOptionDto> options)
    {
        StringBuilder builder = new();

        builder.AppendLine(description);
        builder.AppendLine();
        builder.AppendLine("Selecciona una opción en el menú para ver su imagen, descripción y key.");
        builder.AppendLine();

        foreach (CharacterCatalogOptionDto option in options)
        {
            builder.AppendLine(
                $"`{option.Key}` · **{option.Name}**" +
                (string.IsNullOrWhiteSpace(option.ShortDescription) ? "" : $" — {option.ShortDescription}"));
        }

        return builder.ToString();
    }

    private static string BuildDetailDescription(CharacterCatalogOptionDto option)
    {
        StringBuilder builder = new();

        if (!string.IsNullOrWhiteSpace(option.ShortDescription))
        {
            builder.AppendLine($"**{option.ShortDescription}**");
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(option.Description))
        {
            builder.AppendLine(option.Description);
        }
        else
        {
            builder.AppendLine("Descripción extendida pendiente de escritura.");
        }

        return builder.ToString();
    }

    private static string CatalogEmoji(string catalogType)
    {
        return catalogType switch
        {
            "nation" => "🏳️",
            "role" => "⚔️",
            "profession" => "🧰",
            _ => "📚"
        };
    }
}
