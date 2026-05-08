using System.Collections.Concurrent;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed class InMemoryCharacterCreationSessionStore : ICharacterCreationSessionStore
{
    private readonly ConcurrentDictionary<string, CharacterCreationSessionDto> _sessions = new();

    public CharacterCreationSessionDto Create(
        ulong ownerDiscordUserId,
        string name,
        string? nickname,
        string? imageUrl)
    {
        CleanupExpired();

        string sessionId = Guid.NewGuid().ToString("N")[..10];

        CharacterCreationSessionDto session = new(
            sessionId,
            ownerDiscordUserId,
            name.Trim(),
            string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            0,
            0,
            0,
            0,
            DateTime.UtcNow.AddMinutes(10));

        _sessions[sessionId] = session;

        return session;
    }

    public CharacterCreationSessionDto? Get(
        string sessionId,
        ulong ownerDiscordUserId)
    {
        CleanupExpired();

        if (!_sessions.TryGetValue(sessionId, out CharacterCreationSessionDto? session))
        {
            return null;
        }

        if (session.OwnerDiscordUserId != ownerDiscordUserId)
        {
            return null;
        }

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return session;
    }

    public CharacterCreationSessionDto? UpdateIndexes(
        string sessionId,
        ulong ownerDiscordUserId,
        int? nationIndex = null,
        int? roleIndex = null,
        int? professionIndex = null)
    {
        CharacterCreationSessionDto? current = Get(sessionId, ownerDiscordUserId);

        if (current is null)
        {
            return null;
        }

        CharacterCreationSessionDto updated = current with
        {
            NationIndex = nationIndex ?? current.NationIndex,
            RoleIndex = roleIndex ?? current.RoleIndex,
            ProfessionIndex = professionIndex ?? current.ProfessionIndex
        };

        _sessions[sessionId] = updated;

        return updated;
    }

    public CharacterCreationSessionDto? UpdateStep(
        string sessionId,
        ulong ownerDiscordUserId,
        int currentStep)
    {
        CharacterCreationSessionDto? current = Get(sessionId, ownerDiscordUserId);

        if (current is null)
        {
            return null;
        }

        CharacterCreationSessionDto updated = current with
        {
            CurrentStep = Math.Clamp(currentStep, 0, 2)
        };

        _sessions[sessionId] = updated;

        return updated;
    }

    public void Save(CharacterCreationSessionDto session)
    {
        _sessions[session.SessionId] = session;
    }

    public void Remove(
        string sessionId,
        ulong ownerDiscordUserId)
    {
        CharacterCreationSessionDto? current = Get(sessionId, ownerDiscordUserId);

        if (current is null)
        {
            return;
        }

        _sessions.TryRemove(sessionId, out _);
    }

    private void CleanupExpired()
    {
        DateTime now = DateTime.UtcNow;

        foreach ((string key, CharacterCreationSessionDto session) in _sessions)
        {
            if (session.ExpiresAt <= now)
            {
                _sessions.TryRemove(key, out _);
            }
        }
    }
}
