using Discord;
using Discord.Interactions;

namespace ThaddeusMorrowind.Bot.Modules;

public sealed class CoreModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Comprueba si Thaddeus Morrowind está despierto.")]
    public async Task PingAsync()
    {
        Embed embed = new EmbedBuilder()
            .WithTitle("🏰 Thaddeus Morrowind")
            .WithDescription("Estoy despierto. Pong.")
            .AddField("Estado", "Activo", inline: true)
            .AddField("Modo", "Prueba inicial", inline: true)
            .WithColor(Color.DarkPurple)
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("dado", "Lanza un dado de prueba.")]
    public async Task DadoAsync(
        [Summary(description: "Número de caras del dado. Ejemplo: 6, 20 o 100.")]
        int caras = 20)
    {
        if (caras < 2)
        {
            await RespondAsync(
                "🎲 El dado necesita mínimo 2 caras. No invoquemos geometría prohibida.",
                ephemeral: true);

            return;
        }

        if (caras > 1000)
        {
            await RespondAsync(
                "🎲 Por ahora el dado máximo permitido es de 1000 caras.",
                ephemeral: true);

            return;
        }

        int resultado = Random.Shared.Next(1, caras + 1);

        Embed embed = new EmbedBuilder()
            .WithTitle("🎲 Tirada de dado")
            .WithDescription($"Lanzaste un **d{caras}**.")
            .AddField("Resultado", $"**{resultado}**", inline: true)
            .WithColor(Color.Gold)
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("actividad", "Verifica la actividad inicial del bot.")]
    public async Task ActividadAsync()
    {
        TimeSpan uptime = DateTime.UtcNow - BotRuntime.StartedAtUtc;

        Embed embed = new EmbedBuilder()
            .WithTitle("🦝⚔️ Actividad de Thaddeus Morrowind")
            .WithDescription("Verificación inicial del estado del bot.")
            .AddField("Conexión", "Activa", inline: true)
            .AddField("Comandos", "Registrados", inline: true)
            .AddField("Tiempo encendido", FormatUptime(uptime), inline: false)
            .AddField("Siguiente módulo", "Usuarios y personajes", inline: false)
            .WithColor(Color.Teal)
            .WithFooter("Sistema de actividad inicial")
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }

    [SlashCommand("estado", "Muestra el estado general del proyecto.")]
    public async Task EstadoAsync()
    {
        Embed embed = new EmbedBuilder()
            .WithTitle("📜 Estado del proyecto")
            .WithDescription("Thaddeus Morrowind está en fase base.")
            .AddField("Bot", "Conectado a Discord", inline: false)
            .AddField("Sistema actual", "Ping, dado y actividad", inline: false)
            .AddField("Próximo paso", "Registro de usuario y creación de personaje", inline: false)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }
}