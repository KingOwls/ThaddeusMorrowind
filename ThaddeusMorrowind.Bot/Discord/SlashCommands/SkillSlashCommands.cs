using Discord;
using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Skills;

namespace ThaddeusMorrowind.Bot.Discord.SlashCommands;

[Group("habilidad", "Gestiona el árbol de habilidades de tus personajes.")]
public sealed class SkillSlashCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ISkillTreeService _skillTreeService;

    public SkillSlashCommands(ISkillTreeService skillTreeService)
    {
        _skillTreeService = skillTreeService;
    }

    [SlashCommand("arbol", "Muestra el árbol de habilidades de un personaje.")]
    public async Task TreeAsync(long personaje_id)
    {
        if (personaje_id <= 0)
        {
            await RespondAsync(embed: SkillTreeViews.Error("El ID del personaje debe ser mayor que cero."), ephemeral: true);
            return;
        }

        SkillTreeDto? tree = await _skillTreeService.GetTreeAsync(Context.User.Id, (ulong)personaje_id);

        if (tree is null)
        {
            await RespondAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."), ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: SkillTreeViews.Tree(tree),
            components: SkillTreeComponents.TreeActions(tree.CharacterId),
            ephemeral: true);
    }

    [SlashCommand("lista", "Lista habilidades disponibles para un personaje.")]
    public async Task ListAsync(
        long personaje_id,
        [Summary(description: "Opcional: active, passive, role o stat.")] string? categoria = null)
    {
        if (personaje_id <= 0)
        {
            await RespondAsync(embed: SkillTreeViews.Error("El ID del personaje debe ser mayor que cero."), ephemeral: true);
            return;
        }

        SkillListDto? list = await _skillTreeService.ListSkillsAsync(
            Context.User.Id,
            (ulong)personaje_id,
            categoria,
            onlyKnown: false);

        if (list is null)
        {
            await RespondAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."), ephemeral: true);
            return;
        }

        await RespondAsync(embed: SkillTreeViews.SkillList(list), ephemeral: true);
    }

    [SlashCommand("aprendidas", "Lista habilidades aprendidas por un personaje.")]
    public async Task KnownAsync(
        long personaje_id,
        [Summary(description: "Opcional: active, passive, role o stat.")] string? categoria = null)
    {
        if (personaje_id <= 0)
        {
            await RespondAsync(embed: SkillTreeViews.Error("El ID del personaje debe ser mayor que cero."), ephemeral: true);
            return;
        }

        SkillListDto? list = await _skillTreeService.ListSkillsAsync(
            Context.User.Id,
            (ulong)personaje_id,
            categoria,
            onlyKnown: true);

        if (list is null)
        {
            await RespondAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."), ephemeral: true);
            return;
        }

        await RespondAsync(embed: SkillTreeViews.SkillList(list), ephemeral: true);
    }

    [SlashCommand("obtener", "Otorga/aprende una habilidad para un personaje. Solo administradores.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task GrantAsync(
        long personaje_id,
        long habilidad_id,
        [Summary(description: "Razón opcional.")] string? razon = null)
    {
        if (personaje_id <= 0 || habilidad_id <= 0)
        {
            await RespondAsync(embed: SkillTreeViews.Error("El ID del personaje y de la habilidad deben ser mayores que cero."), ephemeral: true);
            return;
        }

        SkillLearnResult result = await _skillTreeService.GrantSkillAsync(
            Context.User.Id,
            (ulong)personaje_id,
            (ulong)habilidad_id,
            "admin_slash_grant",
            razon,
            allowAnyOwner: true);

        await RespondAsync(embed: SkillTreeViews.LearnResult(result), ephemeral: true);

        if (result.SkillList is not null)
        {
            await FollowupAsync(embed: SkillTreeViews.SkillList(result.SkillList), ephemeral: true);
        }
    }

    [SlashCommand("olvidar", "Quita una habilidad aprendida. También la retira del árbol si estaba equipada. Solo administradores.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task ForgetAsync(
        long personaje_id,
        long habilidad_id,
        [Summary(description: "Razón opcional.")] string? razon = null)
    {
        if (personaje_id <= 0 || habilidad_id <= 0)
        {
            await RespondAsync(embed: SkillTreeViews.Error("El ID del personaje y de la habilidad deben ser mayores que cero."), ephemeral: true);
            return;
        }

        SkillLearnResult result = await _skillTreeService.ForgetSkillAsync(
            Context.User.Id,
            (ulong)personaje_id,
            (ulong)habilidad_id,
            "admin_slash_forget",
            razon,
            allowAnyOwner: true);

        await RespondAsync(embed: SkillTreeViews.LearnResult(result), ephemeral: true);

        if (result.SkillList is not null)
        {
            await FollowupAsync(embed: SkillTreeViews.SkillList(result.SkillList), ephemeral: true);
        }
    }

    [SlashCommand("colocar", "Coloca una habilidad aprendida en un espacio desbloqueado.")]
    public async Task PlaceAsync(long personaje_id, int espacio, long habilidad_id)
    {
        await AssignDirectAsync(personaje_id, espacio, habilidad_id, "place");
    }

    [SlashCommand("reemplazar", "Reemplaza una habilidad en un espacio desbloqueado.")]
    public async Task ReplaceAsync(long personaje_id, int espacio, long habilidad_id)
    {
        await AssignDirectAsync(personaje_id, espacio, habilidad_id, "replace");
    }

    [SlashCommand("quitar", "Quita una habilidad equipada de un espacio. No la olvida.")]
    public async Task RemoveAsync(long personaje_id, int espacio)
    {
        if (personaje_id <= 0 || espacio <= 0)
        {
            await RespondAsync(embed: SkillTreeViews.Error("El ID del personaje y el espacio deben ser mayores que cero."), ephemeral: true);
            return;
        }

        SkillActionResult result = await _skillTreeService.RemoveSkillAsync(
            Context.User.Id,
            (ulong)personaje_id,
            (uint)espacio);

        if (!result.Success)
        {
            await RespondAsync(embed: SkillTreeViews.Error(result.Message), ephemeral: true);
            return;
        }

        await RespondAsync(embed: SkillTreeViews.SkillAction(result), ephemeral: true);

        if (result.Tree is not null)
        {
            await FollowupAsync(
                embed: SkillTreeViews.Tree(result.Tree),
                components: SkillTreeComponents.TreeActions(result.Tree.CharacterId),
                ephemeral: true);
        }
    }

    private async Task AssignDirectAsync(long personajeId, int espacio, long habilidadId, string action)
    {
        if (personajeId <= 0 || espacio <= 0 || habilidadId <= 0)
        {
            await RespondAsync(embed: SkillTreeViews.Error("El ID del personaje, espacio y habilidad deben ser mayores que cero."), ephemeral: true);
            return;
        }

        SkillActionResult result = await _skillTreeService.AssignSkillAsync(
            Context.User.Id,
            (ulong)personajeId,
            (uint)espacio,
            (ulong)habilidadId,
            action);

        if (!result.Success)
        {
            await RespondAsync(embed: SkillTreeViews.Error(result.Message), ephemeral: true);
            return;
        }

        await RespondAsync(embed: SkillTreeViews.SkillAction(result), ephemeral: true);

        if (result.Tree is not null)
        {
            await FollowupAsync(
                embed: SkillTreeViews.Tree(result.Tree),
                components: SkillTreeComponents.TreeActions(result.Tree.CharacterId),
                ephemeral: true);
        }
    }
}
