namespace ThaddeusMorrowind.Bot.Features.Users;

public interface IUserService
{
    Task<UserProfileDto> RegisterOrUpdateAsync(DiscordUserContextDto discordUser, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetProfileAsync(ulong discordUserId, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateDiscordIdentityAsync(DiscordUserContextDto discordUser, CancellationToken cancellationToken = default);
    Task TouchLastInteractionAsync(ulong discordUserId, string activityKey, CancellationToken cancellationToken = default);
}

public interface IUserWalletService
{
    Task<UserWalletDto> EnsureWalletAsync(ulong userAccountId, CancellationToken cancellationToken = default);
    Task<UserWalletDto?> GetWalletAsync(ulong discordUserId, CancellationToken cancellationToken = default);
}

public interface IUserActivityService
{
    Task LogAsync(ulong discordUserId, string activityKey, string? description = null, string? payloadJson = null, CancellationToken cancellationToken = default);
}

public interface IActiveCharacterService
{
    Task<ActiveCharacterDto?> GetActiveCharacterAsync(ulong discordUserId, CancellationToken cancellationToken = default);
    Task<bool> SetActiveCharacterAsync(ulong discordUserId, ulong characterId, CancellationToken cancellationToken = default);
    Task ClearActiveCharacterAsync(ulong discordUserId, CancellationToken cancellationToken = default);
}
