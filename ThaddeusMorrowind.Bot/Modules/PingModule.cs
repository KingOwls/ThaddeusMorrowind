using Discord.Interactions;

namespace ThaddeusMorrowind.Bot.Modules;

public class PingModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Comprueba si Thaddeus Morrowind está despierto.")]
    public async Task PingAsync()
    {
        await RespondAsync("🏰 Thaddeus Morrowind está despierto. Pong.");
    }

    [SlashCommand("estado", "Muestra el estado actual del mini MMORPG.")]
    public async Task EstadoAsync()
    {
        await RespondAsync(
            "🦝⚔️ **Thaddeus Morrowind** está en fase inicial.\n" +
            "Sistema activo: conexión con Discord.\n" +
            "Próximo objetivo: creación de personajes.");
    }

    [SlashCommand("dado", "Lanza un dado de prueba.")]
    public async Task DadoAsync(
        [Summary(description: "Número de caras del dado. Ejemplo: 6, 20 o 100.")]
        int caras = 20)
    {
        if (caras < 2)
        {
            await RespondAsync("El dado necesita mínimo 2 caras, no invoquemos geometría maldita.");
            return;
        }

        int resultado = Random.Shared.Next(1, caras + 1);

        await RespondAsync($"🎲 Lanzaste un d{caras} y salió: **{resultado}**");
    }
}