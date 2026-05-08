using Discord;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Components;

public static class CharacterCreationWizardComponents
{
    public static MessageComponent WizardButtons(CharacterCreationSessionDto session)
    {
        return new ComponentBuilder()
            .WithButton("◀ Nación", $"character:create:nation_prev:{session.SessionId}", ButtonStyle.Secondary, row: 0)
            .WithButton("Nación ▶", $"character:create:nation_next:{session.SessionId}", ButtonStyle.Secondary, row: 0)
            .WithButton("◀ Rol", $"character:create:role_prev:{session.SessionId}", ButtonStyle.Secondary, row: 1)
            .WithButton("Rol ▶", $"character:create:role_next:{session.SessionId}", ButtonStyle.Secondary, row: 1)
            .WithButton("◀ Profesión", $"character:create:profession_prev:{session.SessionId}", ButtonStyle.Secondary, row: 2)
            .WithButton("Profesión ▶", $"character:create:profession_next:{session.SessionId}", ButtonStyle.Secondary, row: 2)
            .WithButton("Confirmar", $"character:create:confirm:{session.SessionId}", ButtonStyle.Success, new Emoji("✅"), row: 3)
            .WithButton("Cancelar", $"character:create:cancel:{session.SessionId}", ButtonStyle.Danger, new Emoji("🧹"), row: 3)
            .Build();
    }
}
