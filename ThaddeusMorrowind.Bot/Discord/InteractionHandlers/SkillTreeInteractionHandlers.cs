using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Skills;

namespace ThaddeusMorrowind.Bot.Discord.InteractionHandlers;

public sealed class SkillTreeInteractionHandlers : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ISkillTreeService _skillTreeService;

    public SkillTreeInteractionHandlers(ISkillTreeService skillTreeService)
    {
        _skillTreeService = skillTreeService;
    }

    [ComponentInteraction("skilltree:action_assign:*")]
    public async Task StartAssignAsync(string characterIdText)
    {
        if (!ulong.TryParse(characterIdText, out ulong characterId))
        {
            await RespondAsync(embed: SkillTreeViews.Error("ID inválido."), ephemeral: true);
            return;
        }

        SkillTreeDto? tree = await _skillTreeService.GetTreeAsync(Context.User.Id, characterId);

        if (tree is null)
        {
            await RespondAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."), ephemeral: true);
            return;
        }

        IReadOnlyList<SkillSlotDto> slots = await _skillTreeService.GetAssignableSlotsAsync(Context.User.Id, characterId);

        if (slots.Count == 0)
        {
            await RespondAsync(embed: SkillTreeViews.Empty("🔒 Sin espacios desbloqueados", "Este personaje aún no tiene espacios disponibles."), ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: SkillTreeViews.ChooseSlot(tree, "assign"),
            components: SkillTreeComponents.AssignSlotSelect(characterId, slots),
            ephemeral: true);
    }

    [ComponentInteraction("skilltree:action_remove:*")]
    public async Task StartRemoveAsync(string characterIdText)
    {
        if (!ulong.TryParse(characterIdText, out ulong characterId))
        {
            await RespondAsync(embed: SkillTreeViews.Error("ID inválido."), ephemeral: true);
            return;
        }

        SkillTreeDto? tree = await _skillTreeService.GetTreeAsync(Context.User.Id, characterId);

        if (tree is null)
        {
            await RespondAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."), ephemeral: true);
            return;
        }

        IReadOnlyList<SkillSlotDto> slots = await _skillTreeService.GetOccupiedSlotsAsync(Context.User.Id, characterId);

        if (slots.Count == 0)
        {
            await RespondAsync(embed: SkillTreeViews.Empty("🧹 Nada que quitar", "Este personaje no tiene habilidades equipadas en el árbol."), ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: SkillTreeViews.ChooseSlot(tree, "remove"),
            components: SkillTreeComponents.RemoveSlotSelect(characterId, slots),
            ephemeral: true);
    }

    [ComponentInteraction("skilltree:slot_assign:*")]
    public async Task SlotAssignSelectedAsync(string characterIdText, string[] selections)
    {
        if (!ulong.TryParse(characterIdText, out ulong characterId) ||
            !uint.TryParse(selections.FirstOrDefault(), out uint slotNumber))
        {
            await RespondAsync(embed: SkillTreeViews.Error("Selección inválida."), ephemeral: true);
            return;
        }

        SkillTreeDto? tree = await _skillTreeService.GetTreeAsync(Context.User.Id, characterId);

        if (tree is null)
        {
            await RespondAsync(embed: SkillTreeViews.Error("No encontré ese personaje entre tus personajes activos."), ephemeral: true);
            return;
        }

        SkillSlotDto? slot = tree.Slots.FirstOrDefault(x => x.SlotNumber == slotNumber);

        if (slot is null)
        {
            await RespondAsync(embed: SkillTreeViews.Error("Ese espacio no existe."), ephemeral: true);
            return;
        }

        IReadOnlyList<SkillTemplateOptionDto> skills = await _skillTreeService.GetAvailableSkillsForSlotAsync(
            Context.User.Id,
            characterId,
            slotNumber);

        if (skills.Count == 0)
        {
            await RespondAsync(
                embed: SkillTreeViews.Empty(
                    "📚 Sin habilidades aprendidas compatibles",
                    "Primero usa `/habilidad lista` para ver IDs disponibles y `/habilidad obtener` para aprender una habilidad."),
                ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: SkillTreeViews.ChooseSkill(tree, slot, skills),
            components: SkillTreeComponents.SkillSelect(characterId, slotNumber, skills),
            ephemeral: true);
    }

    [ComponentInteraction("skilltree:skill_assign:*:*")]
    public async Task SkillAssignSelectedAsync(string characterIdText, string slotNumberText, string[] selections)
    {
        if (!ulong.TryParse(characterIdText, out ulong characterId) ||
            !uint.TryParse(slotNumberText, out uint slotNumber) ||
            !ulong.TryParse(selections.FirstOrDefault(), out ulong skillId))
        {
            await RespondAsync(embed: SkillTreeViews.Error("Selección inválida."), ephemeral: true);
            return;
        }

        SkillActionResult result = await _skillTreeService.AssignSkillAsync(
            Context.User.Id,
            characterId,
            slotNumber,
            skillId,
            "interactive_assign");

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

    [ComponentInteraction("skilltree:slot_remove:*")]
    public async Task SlotRemoveSelectedAsync(string characterIdText, string[] selections)
    {
        if (!ulong.TryParse(characterIdText, out ulong characterId) ||
            !uint.TryParse(selections.FirstOrDefault(), out uint slotNumber))
        {
            await RespondAsync(embed: SkillTreeViews.Error("Selección inválida."), ephemeral: true);
            return;
        }

        SkillActionResult result = await _skillTreeService.RemoveSkillAsync(
            Context.User.Id,
            characterId,
            slotNumber);

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
