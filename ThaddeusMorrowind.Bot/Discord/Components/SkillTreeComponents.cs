using Discord;
using ThaddeusMorrowind.Bot.Features.Skills;

namespace ThaddeusMorrowind.Bot.Discord.Components;

public static class SkillTreeComponents
{
    public static MessageComponent TreeActions(ulong characterId)
    {
        return new ComponentBuilder()
            .WithButton("Colocar/Reemplazar", $"skilltree:action_assign:{characterId}", ButtonStyle.Success, new Emoji("🧩"))
            .WithButton("Quitar", $"skilltree:action_remove:{characterId}", ButtonStyle.Danger, new Emoji("🧹"))
            .Build();
    }

    public static MessageComponent AssignSlotSelect(ulong characterId, IReadOnlyList<SkillSlotDto> slots)
    {
        SelectMenuBuilder menu = new SelectMenuBuilder()
            .WithCustomId($"skilltree:slot_assign:{characterId}")
            .WithPlaceholder("Elige el espacio donde colocar o reemplazar")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (SkillSlotDto slot in slots.Take(25))
        {
            string status = slot.SkillTemplateId is null ? "Vacío" : $"Actual: {slot.SkillName}";
            menu.AddOption(
                label: $"#{slot.SlotNumber:00} · {FormatType(slot.SlotType)} · Nv.{slot.UnlockLevel}",
                value: slot.SlotNumber.ToString(),
                description: Truncate(status, 100),
                emote: new Emoji(TypeEmoji(slot.SlotType)));
        }

        return new ComponentBuilder()
            .WithSelectMenu(menu)
            .Build();
    }

    public static MessageComponent RemoveSlotSelect(ulong characterId, IReadOnlyList<SkillSlotDto> slots)
    {
        SelectMenuBuilder menu = new SelectMenuBuilder()
            .WithCustomId($"skilltree:slot_remove:{characterId}")
            .WithPlaceholder("Elige la habilidad que quieres quitar del árbol")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (SkillSlotDto slot in slots.Take(25))
        {
            menu.AddOption(
                label: $"#{slot.SlotNumber:00} · {slot.SkillName}",
                value: slot.SlotNumber.ToString(),
                description: Truncate(slot.SkillShortDescription ?? slot.SlotName, 100),
                emote: new Emoji("🧹"));
        }

        return new ComponentBuilder()
            .WithSelectMenu(menu)
            .Build();
    }

    public static MessageComponent SkillSelect(ulong characterId, uint slotNumber, IReadOnlyList<SkillTemplateOptionDto> skills)
    {
        SelectMenuBuilder menu = new SelectMenuBuilder()
            .WithCustomId($"skilltree:skill_assign:{characterId}:{slotNumber}")
            .WithPlaceholder("Elige una habilidad aprendida")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (SkillTemplateOptionDto skill in skills.Take(25))
        {
            string cost = skill.ManaCost > 0 ? $" · Mana {skill.ManaCost}" : "";
            string cooldown = skill.CooldownTurns > 0 ? $" · CD {skill.CooldownTurns}" : "";

            menu.AddOption(
                label: Truncate(skill.Name, 100),
                value: skill.Id.ToString(),
                description: Truncate($"ID {skill.Id} · Nv.{skill.RequiredLevel}{cost}{cooldown}", 100),
                emote: new Emoji(TypeEmoji(skill.SkillCategory)));
        }

        return new ComponentBuilder()
            .WithSelectMenu(menu)
            .Build();
    }

    private static string FormatType(string type)
    {
        return type switch
        {
            "active" => "Activa",
            "passive" => "Pasiva",
            "role" => "Rol",
            "stat" => "Stat",
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

    private static string Truncate(string value, int max)
    {
        return value.Length <= max ? value : value[..(max - 3)] + "...";
    }
}
