using ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Inventory;

public interface IInventoryService
{
    Task<InventoryCommandResult<IReadOnlyList<InventoryItemDto>>> GetUserInventoryAsync(ulong discordUserId, CancellationToken cancellationToken = default);

    Task<InventoryCommandResult<IReadOnlyList<InventoryItemDto>>> GetCharacterInventoryAsync(ulong discordUserId, ulong? characterId, string? characterName, CancellationToken cancellationToken = default);

    Task<InventoryCommandResult<EquipmentSummaryDto>> GetEquipmentAsync(ulong requesterDiscordUserId, ulong? ownerDiscordUserId, ulong? characterId, string? characterName, CancellationToken cancellationToken = default);

    Task<InventoryCommandResult<EquipmentSummaryDto>> EquipAsync(ulong discordUserId, ulong characterId, ulong itemInstanceId, CancellationToken cancellationToken = default);

    Task<InventoryCommandResult<EquipmentSummaryDto>> UnequipAsync(ulong discordUserId, ulong characterId, string slotKey, CancellationToken cancellationToken = default);

    Task<InventoryCommandResult<DebugGiveItemDto>> GiveItemAsync(ulong targetDiscordUserId, string itemKey, int quantity, CancellationToken cancellationToken = default);
}
