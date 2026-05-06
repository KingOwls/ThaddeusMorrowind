using Discord.Commands;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Stats;

namespace ThaddeusMorrowind.Bot.Discord.PrefixCommands;

[Group("stats")]
[Alias("stat", "estadisticas", "estadísticas")]
public sealed class StatGrowthPrefixCommands : ModuleBase<SocketCommandContext>
{
    private readonly ICharacterStatGrowthService _statGrowthService;

    public StatGrowthPrefixCommands(ICharacterStatGrowthService statGrowthService)
    {
        _statGrowthService = statGrowthService;
    }

    [Command("ver")]
    [Alias("view")]
    public async Task ViewAsync(ulong personajeId)
    {
        CharacterStatGrowthResult result = await _statGrowthService.GetStatsAsync(personajeId);

        await ReplyAsync(embed: StatGrowthViews.Result(result));
    }

    [Command("recalcular")]
    [Alias("recalc")]
    public async Task RecalculateAsync(ulong personajeId)
    {
        if (!IsAdmin(Context))
        {
            await ReplyAsync("Solo un administrador puede recalcular estadísticas manualmente.");
            return;
        }

        CharacterStatGrowthResult result = await _statGrowthService.RecalculateStatsAsync(personajeId);

        await ReplyAsync(embed: StatGrowthViews.Result(result));
    }

    private static bool IsAdmin(SocketCommandContext context)
    {
        return context.User is SocketGuildUser guildUser
            && guildUser.GuildPermissions.Administrator;
    }
}
