namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed record CharacterSummaryDto(
    ulong Id,
    string Name,
    string? Nickname,
    uint Level,
    ulong Experience,
    string Nation,
    string Profession,
    string Role,
    string? ThumbnailUrl,
    bool IsMainCharacter,
    bool IsActiveCharacter);
