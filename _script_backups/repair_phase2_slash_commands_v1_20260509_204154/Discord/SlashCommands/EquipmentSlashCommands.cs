using Discord;
using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Inventory;
using ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.SlashCommands;

[Group("equipo", "Gestiona arma, artefactos y herramienta única.")]
public sealed class EquipmentSlashCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IInventoryService _inventoryService;

    public EquipmentSlashCommands(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [SlashCommand("ver", "Muestra el equipo de un personaje.")]
    public async Task ViewEquipmentAsync(long personaje_id = 0, string? nombre = null, IUser? usuario = null)
    {
        await DeferAsync(ephemeral: false);

        InventoryCommandResult<EquipmentSummaryDto> result = await _inventoryService.GetEquipmentAsync(
            Context.User.Id,
            usuario?.Id,
            personaje_id > 0 ? (ulong)personaje_id : null,
            nombre);

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? InventoryViews.Equipment(result.Data)
                : InventoryViews.Result("📭 Equipo no encontrado", result.Message, false),
            ephemeral: false);
    }

    [SlashCommand("equipar", "Equipa un item en el personaje indicado.")]
    public async Task EquipAsync(long personaje_id, long item_id)
    {
        await DeferAsync(ephemeral: true);

        if (personaje_id <= 0 || item_id <= 0)
        {
            await FollowupAsync(
                embed: InventoryViews.Result("⚠️ Datos inválidos", "Debes indicar `personaje_id` e `item_id` mayores a 0.", false),
                ephemeral: true);

            return;
        }

        InventoryCommandResult<EquipmentSummaryDto> result = await _inventoryService.EquipAsync(
            Context.User.Id,
            (ulong)personaje_id,
            (ulong)item_id);

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? InventoryViews.Equipment(result.Data)
                : InventoryViews.Result("⚠️ No se pudo equipar", result.Message, false),
            ephemeral: true);
    }

    [SlashCommand("quitar", "Retira el item equipado en un slot.")]
    public async Task UnequipAsync(long personaje_id, string slot)
    {
        await DeferAsync(ephemeral: true);

        if (personaje_id <= 0 || string.IsNullOrWhiteSpace(slot))
        {
            await FollowupAsync(
                embed: InventoryViews.Result("⚠️ Datos inválidos", "Debes indicar `personaje_id` y `slot`.", false),
                ephemeral: true);

            return;
        }

        InventoryCommandResult<EquipmentSummaryDto> result = await _inventoryService.UnequipAsync(
            Context.User.Id,
            (ulong)personaje_id,
            slot.Trim());

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? InventoryViews.Equipment(result.Data)
                : InventoryViews.Result("⚠️ No se pudo quitar", result.Message, false),
            ephemeral: true);
    }
}
