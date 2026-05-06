namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed record CharacterStatDto(
    string Key,
    string Name,
    string ValueKind,
    decimal BaseValue,
    decimal ExtraValue,
    decimal TotalValue);
