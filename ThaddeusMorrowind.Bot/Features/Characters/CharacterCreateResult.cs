namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed record CharacterCreateResult(
    bool Success,
    string Message,
    CharacterDetailDto? Character);
