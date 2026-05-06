using System.Collections.Concurrent;

namespace ThaddeusMorrowind.Bot.Features.Characters.Creation;

public sealed class InMemoryCharacterCreationSessionStore : ICharacterCreationSessionStore
{
    private readonly ConcurrentDictionary<ulong, CharacterCreationSession> _sessions = new();

    public void Start(CharacterCreationSession session)
    {
        _sessions[session.UserId] = session;
    }

    public bool TryGet(ulong userId, out CharacterCreationSession session)
    {
        if (_sessions.TryGetValue(userId, out CharacterCreationSession? found) && !found.IsExpired)
        {
            session = found;
            return true;
        }

        _sessions.TryRemove(userId, out _);

        session = new CharacterCreationSession
        {
            UserId = userId,
            ChannelId = 0
        };

        return false;
    }

    public void Remove(ulong userId)
    {
        _sessions.TryRemove(userId, out _);
    }
}
