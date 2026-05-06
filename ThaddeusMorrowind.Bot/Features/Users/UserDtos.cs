namespace ThaddeusMorrowind.Bot.Features.Users;

public sealed record DiscordUserContextDto(
    ulong DiscordUserId,
    string Username,
    string? PublicNickname,
    string? AvatarUrl);

public sealed record UserProfileDto(
    ulong UserAccountId,
    ulong DiscordUserId,
    string Username,
    string? PublicNickname,
    string? AvatarUrl,
    string AccountStatus,
    uint MaxCharacterSlots,
    uint CharacterCount,
    ulong? ActiveCharacterId,
    string? ActiveCharacterName,
    long Gold,
    long PremiumCurrency,
    DateTime RegisteredAt,
    DateTime? LastInteractionAt);

public sealed record UserWalletDto(
    ulong UserAccountId,
    long Gold,
    long PremiumCurrency,
    DateTime UpdatedAt);

public sealed record ActiveCharacterDto(
    ulong CharacterId,
    string Name,
    string? Nickname,
    string? ImageUrl,
    uint Level,
    string NationName,
    string RoleName,
    string ProfessionName);
