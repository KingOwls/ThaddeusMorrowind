namespace ThaddeusMorrowind.Bot.Features.Characters.Experience;

public sealed record CharacterExperienceResult(
    bool Success,
    string Message,
    ulong? CharacterId,
    string? CharacterName,
    long DeltaExperience,
    ulong PreviousExperience,
    ulong NewExperience,
    uint PreviousLevel,
    uint NewLevel,
    ulong XpForCurrentLevel,
    ulong XpForNextLevel,
    ulong XpProgressInCurrentLevel,
    ulong XpNeededForNextLevel);
