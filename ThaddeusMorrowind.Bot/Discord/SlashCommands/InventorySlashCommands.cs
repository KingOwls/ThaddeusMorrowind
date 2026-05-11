using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Inventory;
using ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.SlashCommands;

[Group("inventario", "Gestiona el inventario de usuario y personaje.")]
public sealed class InventorySlashCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IInventoryService _inventoryService;

    public InventorySlashCommands(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [SlashCommand("ver", "Muestra tu inventario general de usuario.")]
    public async Task ViewUserInventoryAsync()
    {
        await DeferAsync(ephemeral: true);

        InventoryCommandResult<IReadOnlyList<InventoryItemDto>> result = await _inventoryService.GetUserInventoryAsync(Context.User.Id);

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? InventoryViews.Inventory("🎒 Tu inventario", result.Data)
                : InventoryViews.Result("⚠️ Inventario no disponible", result.Message, false),
            ephemeral: true);
    }

    [SlashCommand("personaje", "Muestra el inventario ligado a un personaje tuyo.")]
    public async Task ViewCharacterInventoryAsync(long personaje_id = 0, string? nombre = null)
    {
        await DeferAsync(ephemeral: true);

        InventoryCommandResult<IReadOnlyList<InventoryItemDto>> result = await _inventoryService.GetCharacterInventoryAsync(
            Context.User.Id,
            personaje_id > 0 ? (ulong)personaje_id : null,
            nombre);

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? InventoryViews.Inventory("🧙 Inventario del personaje", result.Data)
                : InventoryViews.Result("⚠️ Inventario no disponible", result.Message, false),
            ephemeral: true);
    }

    [SlashCommand("otorgar", "Admin/debug: otorga un item a un usuario.")]
    public async Task GiveItemAsync(IUser usuario, string item_key, int cantidad = 1)
    {
        await DeferAsync(ephemeral: true);

        bool isAdmin = Context.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator;

        if (!isAdmin)
        {
            await FollowupAsync(
                embed: InventoryViews.Result("⛔ Permiso denegado", "Solo administradores pueden usar este comando.", false),
                ephemeral: true);

            return;
        }

        InventoryCommandResult<DebugGiveItemDto> result = await _inventoryService.GiveItemAsync(usuario.Id, item_key, cantidad);

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? InventoryViews.Give(result.Data)
                : InventoryViews.Result("⚠️ No se pudo otorgar", result.Message, false),
            ephemeral: true);
    }
}
