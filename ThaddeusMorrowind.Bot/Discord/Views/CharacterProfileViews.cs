using Discord;
using System.Text;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class CharacterProfileViews
{
    public static Embed Created(CharacterProfileDto character)
    {
        return Profile(character, "✅ Personaje creado");
    }

    public static Embed Profile(CharacterProfileDto character, string title = "🧙 Ficha de personaje")
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"{title}: {DisplayName(character)}")
            .WithDescription(BuildSummary(character))
            .WithColor(character.CharacterStatus == "active" ? Color.Purple : Color.DarkGrey)
            .AddField("Identidad", BuildIdentity(character), inline: false)
            .AddField("Recursos", BuildResources(character), inline: true)
            .AddField("Equipo", BuildEquipment(character), inline: true)
            .AddField("Árbol de habilidades", BuildTreeSummary(character.SkillTree), inline: false)
            .AddField("Estadísticas principales", BuildMainStats(character.Stats), inline: false)
            .WithFooter($"ID {character.CharacterId} · Thaddeus Morrowind")
            .WithCurrentTimestamp();

        string? thumbnailUrl =
            FirstNotEmpty(character.ImageUrl, character.RoleIconUrl, character.NationIconUrl, character.ProfessionIconUrl);

        string? bannerUrl =
            FirstNotEmpty(character.NationBannerUrl, character.RoleBannerUrl, character.ProfessionBannerUrl);

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            builder.WithThumbnailUrl(thumbnailUrl);
        }

        if (!string.IsNullOrWhiteSpace(bannerUrl))
        {
            builder.WithImageUrl(bannerUrl);
        }

        return builder.Build();
    }

    public static Embed List(IReadOnlyList<CharacterListItemDto> characters, bool includeArchived)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle(includeArchived ? "📋 Tus personajes registrados" : "📋 Tus personajes activos")
            .WithColor(Color.Blue)
            .WithFooter("Thaddeus Morrowind · Personajes")
            .WithCurrentTimestamp();

        if (characters.Count == 0)
        {
            builder.WithDescription("No hay personajes para mostrar.");
            return builder.Build();
        }

        StringBuilder text = new();

        foreach (CharacterListItemDto character in characters)
        {
            string active = character.IsActiveCharacter ? " ⭐ activo" : string.Empty;
            string status = character.CharacterStatus == "active" ? "activo" : character.CharacterStatus;

            text.AppendLine(
                $"`ID {character.CharacterId}` **{DisplayName(character.Name, character.Nickname)}** · Nivel {character.Level} · {character.RoleName}{active}");

            text.AppendLine(
                $"Nación: {character.NationName} · Profesión: {character.ProfessionName} · Estado: `{status}`");

            text.AppendLine();
        }

        builder.WithDescription(text.ToString());
        return builder.Build();
    }

    public static Embed Stats(CharacterProfileDto character)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"📊 Estadísticas de {DisplayName(character)}")
            .WithColor(Color.Teal)
            .WithFooter($"ID {character.CharacterId} · Stats")
            .WithCurrentTimestamp();

        if (!string.IsNullOrWhiteSpace(character.ImageUrl))
        {
            builder.WithThumbnailUrl(character.ImageUrl);
        }

        string mainStats = BuildStatsByKeys(
            character.Stats,
            new[]
            {
                "vida",
                "ataque",
                "poder_magico",
                "armadura",
                "resistencia_magica",
                "velocidad",
                "probabilidad",
                "danio_critico",
                "mana"
            });

        string secondaryStats = BuildStatsByKeys(
            character.Stats,
            new[]
            {
                "evasion",
                "suerte",
                "inmortalidad",
                "bloqueo",
                "aumento_danio_infligido",
                "reduccion_danio_recibido",
                "bono_protectivo",
                "agradecimiento",
                "omnivampirismo"
            });

        builder.AddField(
            "Estadísticas principales",
            string.IsNullOrWhiteSpace(mainStats) ? "Sin estadísticas principales cargadas." : mainStats,
            inline: false);

        builder.AddField(
            "Estadísticas secundarias",
            string.IsNullOrWhiteSpace(secondaryStats) ? "Sin estadísticas secundarias cargadas." : secondaryStats,
            inline: false);

        builder.AddField(
            "Nota de crítico",
            "La probabilidad crítica inicia en **5%** y el daño crítico inicia en **50%**. El daño crítico se muestra como bono, no como 150%.",
            inline: false);

        return builder.Build();
    }

    public static Embed Equipment(CharacterProfileDto character)
    {
        return new EmbedBuilder()
            .WithTitle($"🎒 Equipo de {DisplayName(character)}")
            .WithDescription(
                $"**Arma:** {character.Equipment.WeaponName}\n" +
                $"**Artefactos:** {character.Equipment.ArtifactSummary}\n" +
                $"**ArtUnic:** {character.Equipment.UniqueArtifactName}\n\n" +
                "_La integración real de inventario, armas y artefactos se trabajará en la siguiente versión._")
            .WithColor(Color.DarkBlue)
            .WithFooter($"ID {character.CharacterId} · Equipo")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed SkillTree(CharacterProfileDto character)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"🌳 Árbol de habilidades de {DisplayName(character)}")
            .WithColor(Color.Green)
            .WithFooter($"ID {character.CharacterId} · Árbol")
            .WithCurrentTimestamp();

        StringBuilder text = new();

        foreach (CharacterSkillSlotDto slot in character.SkillTree.Slots)
        {
            string lockIcon = slot.IsUnlocked ? "🔓" : "🔒";
            string equipped = slot.SkillName is null ? "_Vacío_" : $"**{slot.SkillName}**";
            text.AppendLine(
                $"{lockIcon} `Slot {slot.SlotNumber}` {slot.SlotName} · `{slot.SlotCategory}` · Nivel {slot.UnlockLevel} → {equipped}");
        }

        builder.WithDescription(text.Length == 0 ? "No hay slots configurados." : text.ToString());
        return builder.Build();
    }

    public static Embed Result(string title, string message, bool success)
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .WithColor(success ? Color.Green : Color.Red)
            .WithFooter("Thaddeus Morrowind · Personajes")
            .WithCurrentTimestamp()
            .Build();
    }

    private static string BuildSummary(CharacterProfileDto character)
    {
        string active = character.IsActiveCharacter ? "⭐ Personaje activo" : "Personaje registrado";
        return $"{active} · Estado `{character.CharacterStatus}` · Dueño: **{character.OwnerUsername}**";
    }

    private static string BuildIdentity(CharacterProfileDto character)
    {
        return
            $"**Nivel:** {character.Level}\n" +
            $"**Nación:** {character.NationName} `key: {character.NationKey}`\n" +
            $"**Rol:** {character.RoleName} `key: {character.RoleKey}`\n" +
            $"**Profesión:** {character.ProfessionName} `key: {character.ProfessionKey}`";
    }

    private static string BuildResources(CharacterProfileDto character)
    {
        return
            $"**XP actual:** {character.CurrentXp}\n" +
            $"**XP total:** {character.TotalXp}";
    }

    private static string BuildEquipment(CharacterProfileDto character)
    {
        return
            $"**Arma:** {character.Equipment.WeaponName}\n" +
            $"**Artefactos:** {character.Equipment.ArtifactSummary}\n" +
            $"**ArtUnic:** {character.Equipment.UniqueArtifactName}";
    }

    private static string BuildTreeSummary(CharacterSkillTreeSummaryDto tree)
    {
        return
            $"Espacios desbloqueados: **{tree.UnlockedSlots}/{tree.TotalSlots}**\n" +
            $"Habilidades equipadas: **{tree.EquippedSlots}/{tree.TotalSlots}**";
    }

    private static string BuildMainStats(IReadOnlyList<CharacterStatValueDto> stats)
    {
        return BuildStatsByKeys(
            stats,
            new[]
            {
                "vida",
                "ataque",
                "poder_magico",
                "armadura",
                "resistencia_magica",
                "velocidad",
                "probabilidad",
                "danio_critico",
                "mana"
            });
    }

    private static string BuildStatsByKeys(
        IReadOnlyList<CharacterStatValueDto> stats,
        IReadOnlyList<string> keys)
    {
        StringBuilder text = new();

        foreach (string key in keys)
        {
            CharacterStatValueDto? stat = stats.FirstOrDefault(s => s.StatKey == key);

            if (stat is null)
            {
                continue;
            }

            text.AppendLine(
                $"**{stat.Name}:** {FormatValue(stat.TotalValue, stat.ValueKind)} " +
                $"`base {FormatValue(stat.BaseValue, stat.ValueKind)} + extra {FormatValue(stat.ExtraValue, stat.ValueKind)}`");
        }

        return text.ToString();
    }

    private static string DisplayName(CharacterProfileDto character)
    {
        return DisplayName(character.Name, character.Nickname);
    }

    private static string DisplayName(string name, string? nickname)
    {
        return string.IsNullOrWhiteSpace(nickname)
            ? name
            : $"{name} · {nickname}";
    }


    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string FormatValue(decimal value, string valueKind)
    {
        decimal rounded = decimal.Round(value, 2);
        string text = rounded.ToString("0.##");

        return valueKind switch
        {
            "percent" => $"{text}%",
            "multiplier" => $"{text}x",
            _ => text
        };
    }
}
