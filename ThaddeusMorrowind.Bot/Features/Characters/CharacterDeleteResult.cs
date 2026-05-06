namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed record CharacterDeleteResult(
    bool Success,
    string Message,
    ulong? DeletedCharacterId,
    string? DeletedCharacterName);
