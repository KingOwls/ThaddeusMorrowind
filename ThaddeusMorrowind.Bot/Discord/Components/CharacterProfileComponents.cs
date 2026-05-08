using Discord;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Components;

public static class CharacterProfileComponents
{
    public static MessageComponent ForOwner(CharacterProfileDto character)
    {
        return new ComponentBuilder()
            .WithButton("Seleccionar", $"character:select:{character.CharacterId}", ButtonStyle.Success, new Emoji("⭐"))
            .WithButton("Stats", $"character:stats:{character.CharacterId}", ButtonStyle.Primary, new Emoji("📊"))
            .WithButton("Árbol", $"character:tree:{character.CharacterId}", ButtonStyle.Primary, new Emoji("🌳"))
            .WithButton("Equipo", $"character:equipment:{character.CharacterId}", ButtonStyle.Secondary, new Emoji("🎒"))
            .WithButton("Archivar", $"character:archive:{character.CharacterId}", ButtonStyle.Danger, new Emoji("📦"))
            .Build();
    }

    public static MessageComponent ForPublic(CharacterProfileDto character)
    {
        return new ComponentBuilder()
            .WithButton("Stats", $"character:stats:{character.CharacterId}", ButtonStyle.Primary, new Emoji("📊"))
            .WithButton("Árbol", $"character:tree:{character.CharacterId}", ButtonStyle.Primary, new Emoji("🌳"))
            .WithButton("Equipo", $"character:equipment:{character.CharacterId}", ButtonStyle.Secondary, new Emoji("🎒"))
            .Build();
    }
}
