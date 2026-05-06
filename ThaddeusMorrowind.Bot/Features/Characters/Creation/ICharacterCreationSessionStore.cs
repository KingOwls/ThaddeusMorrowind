namespace ThaddeusMorrowind.Bot.Features.Characters.Creation;

public interface ICharacterCreationSessionStore
{
    void Start(CharacterCreationSession session);

    bool TryGet(ulong userId, out CharacterCreationSession session);

    void Remove(ulong userId);
}
