namespace ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

public sealed record InventoryCommandResult<T>(bool Success, string Message, T? Data)
{
    public static InventoryCommandResult<T> Ok(T data, string message = "Operación completada.")
    {
        return new InventoryCommandResult<T>(true, message, data);
    }

    public static InventoryCommandResult<T> Fail(string message)
    {
        return new InventoryCommandResult<T>(false, message, default);
    }
}

public sealed record InventoryItemDto(
    ulong ItemInstanceId,
    string ItemKey,
    string Name,
    string CategoryKey,
    string CategoryName,
    string? ItemSubtype,
    string? RarityName,
    int? Stars,
    int Quantity,
    string Location,
    int Level,
    int Refinement,
    string QualityRank,
    bool IsLocked,
    string? IconUrl,
    string? ShortDescription);

public sealed record EquipmentSlotDto(
    string SlotKey,
    string SlotName,
    string SlotGroup,
    bool CountsForArtifactSet,
    InventoryItemDto? Item);

public sealed record EquipmentSummaryDto(
    ulong CharacterId,
    string CharacterName,
    string? CharacterImageUrl,
    IReadOnlyList<EquipmentSlotDto> Slots);

public sealed record DebugGiveItemDto(
    ulong ItemInstanceId,
    string ItemName,
    int Quantity,
    string OwnerUsername);
