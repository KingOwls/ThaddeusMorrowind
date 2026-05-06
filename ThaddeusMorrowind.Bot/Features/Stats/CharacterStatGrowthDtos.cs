namespace ThaddeusMorrowind.Bot.Features.Stats;

public sealed record CharacterStatGrowthResult(
    bool Success,
    string Message,
    ulong? CharacterId,
    string? CharacterName,
    uint? Level,
    string? NationKey,
    string? RoleKey,
    string? ProfessionKey,
    IReadOnlyList<CharacterStatGrowthValueDto> Stats);

public sealed record CharacterStatGrowthValueDto(
    string StatKey,
    string Name,
    string ValueKind,
    decimal BaseValue,
    decimal ExtraValue,
    decimal TotalValue);
