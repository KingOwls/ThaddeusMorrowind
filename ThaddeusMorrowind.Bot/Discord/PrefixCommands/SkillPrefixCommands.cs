using Discord.Commands;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Skills;

namespace ThaddeusMorrowind.Bot.Discord.PrefixCommands;

[Group("habilidad")]
[Alias("hab", "skill")]
public sealed class SkillPrefixCommands : ModuleBase<SocketCommandContext>
{
    private readonly ISkillTreeService _skillTreeService;

    public SkillPrefixCommands(ISkillTreeService skillTreeService)
    {
        _skillTreeService = skillTreeService;
    }

    [Command("arbol")]
    [Alias("tree")]
    public async Task TreeAsync(ulong personajeId)
    {
        SkillTreeDto? tree = await _skillTreeService.GetTreeAsync(Context.User.Id, personajeId);

        if (tree is null)
        {
            await ReplyAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."));
            return;
        }

        await ReplyAsync(
            embed: SkillTreeViews.Tree(tree),
            components: SkillTreeComponents.TreeActions(tree.CharacterId));
    }

    [Command("lista")]
    [Alias("list")]
    public async Task ListAsync(ulong personajeId, string? categoria = null)
    {
        SkillListDto? list = await _skillTreeService.ListSkillsAsync(
            Context.User.Id,
            personajeId,
            categoria,
            onlyKnown: false);

        if (list is null)
        {
            await ReplyAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."));
            return;
        }

        await ReplyAsync(embed: SkillTreeViews.SkillList(list));
    }

    [Command("aprendidas")]
    [Alias("known")]
    public async Task KnownAsync(ulong personajeId, string? categoria = null)
    {
        SkillListDto? list = await _skillTreeService.ListSkillsAsync(
            Context.User.Id,
            personajeId,
            categoria,
            onlyKnown: true);

        if (list is null)
        {
            await ReplyAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."));
            return;
        }

        await ReplyAsync(embed: SkillTreeViews.SkillList(list));
    }

    [Command("obtener")]
    [Alias("grant")]
    public async Task GrantAsync(ulong personajeId, ulong habilidadId, [Remainder] string? razon = null)
    {
        if (!IsAdmin(Context))
        {
            await ReplyAsync(embed: SkillTreeViews.Error("Solo un administrador puede otorgar habilidades manualmente."));
            return;
        }

        SkillLearnResult result = await _skillTreeService.GrantSkillAsync(
            Context.User.Id,
            personajeId,
            habilidadId,
            "admin_prefix_grant",
            razon,
            allowAnyOwner: true);

        await ReplyAsync(embed: SkillTreeViews.LearnResult(result));

        if (result.SkillList is not null)
        {
            await ReplyAsync(embed: SkillTreeViews.SkillList(result.SkillList));
        }
    }

    [Command("olvidar")]
    [Alias("forget")]
    public async Task ForgetAsync(ulong personajeId, ulong habilidadId, [Remainder] string? razon = null)
    {
        if (!IsAdmin(Context))
        {
            await ReplyAsync(embed: SkillTreeViews.Error("Solo un administrador puede quitar habilidades aprendidas manualmente."));
            return;
        }

        SkillLearnResult result = await _skillTreeService.ForgetSkillAsync(
            Context.User.Id,
            personajeId,
            habilidadId,
            "admin_prefix_forget",
            razon,
            allowAnyOwner: true);

        await ReplyAsync(embed: SkillTreeViews.LearnResult(result));

        if (result.SkillList is not null)
        {
            await ReplyAsync(embed: SkillTreeViews.SkillList(result.SkillList));
        }
    }

    [Command("colocar")]
    [Alias("place")]
    public async Task PlaceAsync(ulong personajeId, uint espacio, ulong habilidadId)
    {
        await AssignAsync(personajeId, espacio, habilidadId, "prefix_place");
    }

    [Command("reemplazar")]
    [Alias("replace")]
    public async Task ReplaceAsync(ulong personajeId, uint espacio, ulong habilidadId)
    {
        await AssignAsync(personajeId, espacio, habilidadId, "prefix_replace");
    }

    [Command("quitar")]
    [Alias("remove")]
    public async Task RemoveAsync(ulong personajeId, uint espacio)
    {
        SkillActionResult result = await _skillTreeService.RemoveSkillAsync(
            Context.User.Id,
            personajeId,
            espacio);

        if (!result.Success)
        {
            await ReplyAsync(embed: SkillTreeViews.Error(result.Message));
            return;
        }

        await ReplyAsync(embed: SkillTreeViews.SkillAction(result));

        if (result.Tree is not null)
        {
            await ReplyAsync(
                embed: SkillTreeViews.Tree(result.Tree),
                components: SkillTreeComponents.TreeActions(result.Tree.CharacterId));
        }
    }

    private async Task AssignAsync(ulong personajeId, uint espacio, ulong habilidadId, string action)
    {
        SkillActionResult result = await _skillTreeService.AssignSkillAsync(
            Context.User.Id,
            personajeId,
            espacio,
            habilidadId,
            action);

        if (!result.Success)
        {
            await ReplyAsync(embed: SkillTreeViews.Error(result.Message));
            return;
        }

        await ReplyAsync(embed: SkillTreeViews.SkillAction(result));

        if (result.Tree is not null)
        {
            await ReplyAsync(
                embed: SkillTreeViews.Tree(result.Tree),
                components: SkillTreeComponents.TreeActions(result.Tree.CharacterId));
        }
    }

    private static bool IsAdmin(SocketCommandContext context)
    {
        return context.User is SocketGuildUser guildUser
            && guildUser.GuildPermissions.Administrator;
    }
}
