namespace ThaddeusMorrowind.Bot.Features.Characters.Experience;

public interface ICharacterExperienceService
{
    Task<CharacterExperienceResult> AdjustExperienceAsync(
        ulong characterId,
        long deltaExperience,
        ulong actorDiscordUserId,
        string sourceKey,
        string? reason,
        CancellationToken cancellationToken = default);
}
