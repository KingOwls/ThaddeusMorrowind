using Discord.Commands;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters.Experience;

namespace ThaddeusMorrowind.Bot.Discord.PrefixCommands;

[Group("pjxp")]
[Alias("personaje_xp")]
public sealed class CharacterExperiencePrefixCommands : ModuleBase<SocketCommandContext>
{
    private readonly ICharacterExperienceService _characterExperienceService;

    public CharacterExperiencePrefixCommands(ICharacterExperienceService characterExperienceService)
    {
        _characterExperienceService = characterExperienceService;
    }

    [Command("agregar")]
    [Alias("add", "sumar")]
    public async Task AddAsync(ulong id, long cantidad, [Remainder] string? razon = null)
    {
        if (!IsAdmin(Context))
        {
            await ReplyAsync(embed: CharacterViews.Error("Solo un administrador puede modificar experiencia."));
            return;
        }

        if (cantidad <= 0)
        {
            await ReplyAsync(embed: CharacterViews.Error("La cantidad debe ser mayor que cero."));
            return;
        }

        CharacterExperienceResult result = await _characterExperienceService.AdjustExperienceAsync(
            id,
            cantidad,
            Context.User.Id,
            "admin_prefix_add_xp",
            razon);

        await ReplyAsync(embed: CharacterExperienceViews.Result(result));
    }

    [Command("quitar")]
    [Alias("remove", "restar")]
    public async Task RemoveAsync(ulong id, long cantidad, [Remainder] string? razon = null)
    {
        if (!IsAdmin(Context))
        {
            await ReplyAsync(embed: CharacterViews.Error("Solo un administrador puede modificar experiencia."));
            return;
        }

        if (cantidad <= 0)
        {
            await ReplyAsync(embed: CharacterViews.Error("La cantidad debe ser mayor que cero."));
            return;
        }

        CharacterExperienceResult result = await _characterExperienceService.AdjustExperienceAsync(
            id,
            -cantidad,
            Context.User.Id,
            "admin_prefix_remove_xp",
            razon);

        await ReplyAsync(embed: CharacterExperienceViews.Result(result));
    }

    private static bool IsAdmin(SocketCommandContext context)
    {
        return context.User is SocketGuildUser guildUser
            && guildUser.GuildPermissions.Administrator;
    }
}
