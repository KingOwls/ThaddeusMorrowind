using Discord;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Components;

public static class CharacterCreationWizardComponents
{
    public static MessageComponent WizardButtons(CharacterCreationSessionDto session)
    {
        string acceptLabel = session.CurrentStep switch
        {
            0 => "Aceptar nación",
            1 => "Aceptar rol",
            2 => "Crear personaje",
            _ => "Aceptar"
        };

        Emoji acceptEmoji = session.CurrentStep == 2
            ? new Emoji("✅")
            : new Emoji("➡️");

        return new ComponentBuilder()
            .WithButton("◀", $"character:create:prev:{session.SessionId}", ButtonStyle.Secondary, row: 0)
            .WithButton(acceptLabel, $"character:create:accept:{session.SessionId}", ButtonStyle.Success, acceptEmoji, row: 0)
            .WithButton("▶", $"character:create:next:{session.SessionId}", ButtonStyle.Secondary, row: 0)
            .WithButton("Cancelar", $"character:create:cancel:{session.SessionId}", ButtonStyle.Danger, new Emoji("🧹"), row: 1)
            .Build();
    }
}
