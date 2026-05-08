using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Characters;

public interface ICharacterCreationSessionStore
{
    CharacterCreationSessionDto Create(
        ulong ownerDiscordUserId,
        string name,
        string? nickname,
        string? imageUrl);

    CharacterCreationSessionDto? Get(
        string sessionId,
        ulong ownerDiscordUserId);

    CharacterCreationSessionDto? UpdateIndexes(
        string sessionId,
        ulong ownerDiscordUserId,
        int? nationIndex = null,
        int? roleIndex = null,
        int? professionIndex = null);

    CharacterCreationSessionDto? UpdateStep(
        string sessionId,
        ulong ownerDiscordUserId,
        int currentStep);

    void Save(CharacterCreationSessionDto session);

    void Remove(
        string sessionId,
        ulong ownerDiscordUserId);
}
