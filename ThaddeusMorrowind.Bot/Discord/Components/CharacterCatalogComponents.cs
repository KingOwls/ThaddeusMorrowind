using Discord;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Components;

public static class CharacterCatalogComponents
{
    public static MessageComponent CatalogMenu(
        string catalogType,
        IReadOnlyList<CharacterCatalogOptionDto> options)
    {
        SelectMenuBuilder menu = new SelectMenuBuilder()
            .WithCustomId($"character:catalog:{catalogType}:select")
            .WithPlaceholder($"Selecciona {HumanCatalogName(catalogType)}")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (CharacterCatalogOptionDto option in options.Take(25))
        {
            string label = option.Name.Length > 100
                ? option.Name[..100]
                : option.Name;

            string? description = option.ShortDescription;

            if (!string.IsNullOrWhiteSpace(description) && description.Length > 100)
            {
                description = description[..100];
            }

            menu.AddOption(
                label,
                option.Key,
                description);
        }

        return new ComponentBuilder()
            .WithSelectMenu(menu)
            .Build();
    }

    private static string HumanCatalogName(string catalogType)
    {
        return catalogType switch
        {
            "nation" => "una nación",
            "role" => "un rol",
            "profession" => "una profesión",
            _ => "una opción"
        };
    }
}
