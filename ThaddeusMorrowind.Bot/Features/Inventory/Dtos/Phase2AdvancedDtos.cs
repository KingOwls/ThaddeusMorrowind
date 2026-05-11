namespace ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

public sealed record Phase2Result<T>(bool Success, string Message, T? Data)
{
    public static Phase2Result<T> Ok(T data, string message = "Operación completada.") => new(true, message, data);
    public static Phase2Result<T> Fail(string message) => new(false, message, default);
}

public sealed record CharacterXpDto(ulong CharacterId, string CharacterName, uint Level, ulong CurrentXp, ulong TotalXp, ulong XpToNextLevel);
public sealed record ArtifactSetDto(string SetKey, string SetType, string? SourceKey, string Name, string? Description, string? TwoPieceSummary, string? FourPieceSummary, IReadOnlyList<string> Bonuses);
public sealed record WeaponDto(string ItemKey, string Name, string? Description, string? RarityName, int? Stars, string? MainStat, string? SecondaryStat, IReadOnlyList<string> Passives);
public sealed record ItemTemplateSearchDto(string ItemKey, string Name, string CategoryKey, string? ItemSubtype, string? RarityName, int? Stars, string? Description);
public sealed record ArtifactXpDto(ulong ItemInstanceId, string ItemName, int OldLevel, int NewLevel, ulong OldXp, ulong NewXp, int MaxLevel, IReadOnlyList<string> Events);
public sealed record FinalStatDto(string StatKey, string Name, string ValueKind, decimal BaseValue, decimal FlatBonus, decimal PercentBonus, decimal FinalValue);
public sealed record FinalStatsDto(ulong CharacterId, string CharacterName, IReadOnlyList<FinalStatDto> Stats);
