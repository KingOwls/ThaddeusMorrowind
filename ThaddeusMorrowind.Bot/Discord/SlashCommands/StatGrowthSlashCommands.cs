using Discord;
using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Stats;

namespace ThaddeusMorrowind.Bot.Discord.SlashCommands;

[Group("stats", "Consulta y recalcula estadísticas de personajes.")]
public sealed class StatGrowthSlashCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICharacterStatGrowthService _statGrowthService;

    public StatGrowthSlashCommands(ICharacterStatGrowthService statGrowthService)
    {
        _statGrowthService = statGrowthService;
    }

    [SlashCommand("ver", "Muestra las estadísticas actuales de un personaje.")]
    public async Task ViewAsync(long personaje_id)
    {
        if (personaje_id <= 0)
        {
            await RespondAsync("El ID del personaje debe ser mayor que cero.", ephemeral: true);
            return;
        }

        CharacterStatGrowthResult result = await _statGrowthService.GetStatsAsync((ulong)personaje_id);

        await RespondAsync(embed: StatGrowthViews.Result(result), ephemeral: true);
    }

    [SlashCommand("recalcular", "Recalcula estadísticas por nivel, nación, rol y profesión. Solo administradores.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task RecalculateAsync(long personaje_id)
    {
        if (personaje_id <= 0)
        {
            await RespondAsync("El ID del personaje debe ser mayor que cero.", ephemeral: true);
            return;
        }

        CharacterStatGrowthResult result = await _statGrowthService.RecalculateStatsAsync((ulong)personaje_id);

        await RespondAsync(embed: StatGrowthViews.Result(result), ephemeral: true);
    }
}
