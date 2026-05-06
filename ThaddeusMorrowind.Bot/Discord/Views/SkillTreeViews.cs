using System.Text;
using Discord;
using ThaddeusMorrowind.Bot.Discord.Views.Shared;
using ThaddeusMorrowind.Bot.Features.Skills;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class SkillTreeViews
{
    public static Embed Tree(SkillTreeDto tree)
    {
        StringBuilder description = new();

        description.AppendLine($"Personaje: **{tree.CharacterName}**");
        description.AppendLine($"Nivel: **{tree.Level}**");
        description.AppendLine();

        foreach (SkillSlotDto slot in tree.Slots)
        {
            string lockText = slot.IsUnlocked ? "✅" : "🔒";
            string type = FormatType(slot.SlotType);
            string skill = slot.SkillName is null ? "_Vacío_" : $"**{slot.SkillName}**";

            description.AppendLine(
                $"{lockText} `#{slot.SlotNumber:00}` {TypeEmoji(slot.SlotType)} **{type}** · Nv.{slot.UnlockLevel} · {skill}");
        }

        return new EmbedBuilder()
            .WithTitle("🌌 Árbol de habilidades")
            .WithDescription(description.ToString())
            .WithColor(EmbedPalette.ArcanePurple)
            .WithFooter("Thaddeus Morrowind · Árbol de habilidades")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed SkillList(SkillListDto list)
    {
        StringBuilder description = new();

        description.AppendLine($"Personaje: **{list.CharacterName}** · Nivel **{list.Level}**");
        description.AppendLine(list.OnlyKnown ? "Mostrando: **habilidades aprendidas**" : "Mostrando: **habilidades disponibles por nivel/origen**");

        if (!string.IsNullOrWhiteSpace(list.CategoryFilter))
        {
            description.AppendLine($"Filtro: **{FormatType(list.CategoryFilter)}**");
        }

        description.AppendLine();

        if (list.Skills.Count == 0)
        {
            description.AppendLine("_No hay habilidades para mostrar._");
        }
        else
        {
            foreach (SkillTemplateOptionDto skill in list.Skills.Take(40))
            {
                string known = skill.IsKnown ? "✅" : "▫️";
                string origin = skill.OriginType == "global"
                    ? "global"
                    : $"{skill.OriginType}:{skill.OriginKey}";

                description.AppendLine(
                    $"{known} `ID {skill.Id}` {TypeEmoji(skill.SkillCategory)} **{skill.Name}** · {FormatType(skill.SkillCategory)} · Nv.{skill.RequiredLevel} · `{origin}`");
            }

            if (list.Skills.Count > 40)
            {
                description.AppendLine();
                description.AppendLine($"_Mostrando 40 de {list.Skills.Count} resultados._");
            }
        }

        return new EmbedBuilder()
            .WithTitle(list.OnlyKnown ? "📘 Habilidades aprendidas" : "📚 Lista de habilidades disponibles")
            .WithDescription(description.ToString())
            .WithColor(list.OnlyKnown ? EmbedPalette.SuccessGreen : EmbedPalette.CharacterBlue)
            .WithFooter("Usa el ID para /habilidad obtener, colocar u olvidar")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed LearnResult(SkillLearnResult result)
    {
        return new EmbedBuilder()
            .WithTitle(result.Success ? "✅ Habilidad actualizada" : "⚠️ No se pudo actualizar habilidad")
            .WithDescription(result.Message)
            .WithColor(result.Success ? EmbedPalette.SuccessGreen : EmbedPalette.ErrorRed)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed ChooseSlot(SkillTreeDto tree, string action)
    {
        string actionText = action == "remove"
            ? "Elige qué habilidad quieres quitar del árbol. La habilidad seguirá aprendida."
            : "Elige el espacio donde quieres colocar o reemplazar una habilidad aprendida.";

        return new EmbedBuilder()
            .WithTitle("🧩 Selección de espacio")
            .WithDescription($"Personaje: **{tree.CharacterName}**\n{actionText}")
            .WithColor(action == "remove" ? EmbedPalette.WarningOrange : EmbedPalette.SuccessGreen)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed ChooseSkill(SkillTreeDto tree, SkillSlotDto slot, IReadOnlyList<SkillTemplateOptionDto> skills)
    {
        return new EmbedBuilder()
            .WithTitle($"📚 Habilidades aprendidas para espacio #{slot.SlotNumber:00}")
            .WithDescription(
                $"Personaje: **{tree.CharacterName}**\n" +
                $"Tipo de espacio: **{FormatType(slot.SlotType)}**\n" +
                $"Opciones disponibles: **{skills.Count}**")
            .WithColor(EmbedPalette.CharacterBlue)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed SkillAction(SkillActionResult result)
    {
        if (!result.Success)
        {
            return Error(result.Message);
        }

        return new EmbedBuilder()
            .WithTitle("✅ Árbol actualizado")
            .WithDescription(result.Message)
            .WithColor(EmbedPalette.SuccessGreen)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed Empty(string title, string message)
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .WithColor(EmbedPalette.WarningOrange)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed Error(string message)
    {
        return new EmbedBuilder()
            .WithTitle("⚠️ No se pudo completar la acción")
            .WithDescription(message)
            .WithColor(EmbedPalette.ErrorRed)
            .WithCurrentTimestamp()
            .Build();
    }

    private static string FormatType(string type)
    {
        return type switch
        {
            "active" => "Activa",
            "passive" => "Pasiva",
            "role" => "Rol/Exploración",
            "stat" => "Estadística",
            _ => type
        };
    }

    private static string TypeEmoji(string type)
    {
        return type switch
        {
            "active" => "⚔️",
            "passive" => "🌙",
            "role" => "🧭",
            "stat" => "💠",
            _ => "🧩"
        };
    }
}
