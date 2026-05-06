using Discord;
using ThaddeusMorrowind.Bot.Features.Characters;

namespace ThaddeusMorrowind.Bot.Discord.Components;

public static class CharacterCreationComponents
{
    public const string StartButtonId = "character:create:start";
    public const string NationSelectId = "character:create:nation";
    public const string ProfessionSelectId = "character:create:profession";
    public const string RoleSelectId = "character:create:role";

    public static MessageComponent StartButton()
    {
        return new ComponentBuilder()
            .WithButton(
                label: "Iniciar creación",
                customId: StartButtonId,
                style: ButtonStyle.Success,
                emote: new Emoji("✨"))
            .Build();
    }

    public static MessageComponent NationSelect(IReadOnlyList<CharacterCatalogOptionDto> options)
    {
        return BuildSelect(
            NationSelectId,
            "Elige una nación",
            options,
            "🏛️");
    }

    public static MessageComponent ProfessionSelect(IReadOnlyList<CharacterCatalogOptionDto> options)
    {
        return BuildSelect(
            ProfessionSelectId,
            "Elige una profesión",
            options,
            "🧰");
    }

    public static MessageComponent RoleSelect(IReadOnlyList<CharacterCatalogOptionDto> options)
    {
        return BuildSelect(
            RoleSelectId,
            "Elige un rol",
            options,
            "⚔️");
    }

    public static MessageComponent CharacterActions(ulong characterId)
    {
        return new ComponentBuilder()
            .WithButton("Seleccionar", $"character:select:{characterId}", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Ver stats completos", $"character:stats:{characterId}", ButtonStyle.Primary, new Emoji("📊"))
            .WithButton("Artefactos", $"character:artifacts:{characterId}", ButtonStyle.Secondary, new Emoji("🏺"))
            .WithButton("Habilidades", $"character:skills:{characterId}", ButtonStyle.Secondary, new Emoji("✨"))
            .Build();
    }

    private static MessageComponent BuildSelect(
        string customId,
        string placeholder,
        IReadOnlyList<CharacterCatalogOptionDto> options,
        string fallbackEmoji)
    {
        SelectMenuBuilder menu = new SelectMenuBuilder()
            .WithCustomId(customId)
            .WithPlaceholder(placeholder)
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (CharacterCatalogOptionDto option in options.Take(25))
        {
            string description = option.ShortDescription ?? option.Description ?? option.Key;

            if (description.Length > 100)
            {
                description = description[..97] + "...";
            }

            menu.AddOption(
                label: option.Name.Length <= 100 ? option.Name : option.Name[..100],
                value: option.Id.ToString(),
                description: description,
                emote: new Emoji(fallbackEmoji));
        }

        return new ComponentBuilder()
            .WithSelectMenu(menu)
            .Build();
    }
}
