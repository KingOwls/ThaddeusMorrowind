namespace ThaddeusMorrowind.Bot.Features.Characters.Dtos;

public sealed record CharacterCreationSessionDto(
    string SessionId,
    ulong OwnerDiscordUserId,
    string Name,
    string? Nickname,
    string? ImageUrl,
    int NationIndex,
    int RoleIndex,
    int ProfessionIndex,
    DateTime ExpiresAt);
