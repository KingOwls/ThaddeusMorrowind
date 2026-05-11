using ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Inventory;

public interface IPhase2AdvancedService
{
    Task<Phase2Result<CharacterXpDto>> AddCharacterXpAsync(ulong actorDiscordUserId, ulong? characterId, string? characterName, long amount, bool allowAnyOwner, CancellationToken cancellationToken = default);
    Task<Phase2Result<CharacterXpDto>> RemoveCharacterXpAsync(ulong actorDiscordUserId, ulong? characterId, string? characterName, long amount, bool allowAnyOwner, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArtifactSetDto>> ListArtifactSetsAsync(string? filter, CancellationToken cancellationToken = default);
    Task<ArtifactSetDto?> GetArtifactSetAsync(string setKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WeaponDto>> ListWeaponsAsync(string? filter, CancellationToken cancellationToken = default);
    Task<WeaponDto?> GetWeaponAsync(string itemKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemTemplateSearchDto>> SearchItemsAsync(string? term, string? categoryKey, CancellationToken cancellationToken = default);
    Task<Phase2Result<ArtifactXpDto>> AddArtifactXpAsync(ulong discordUserId, ulong itemInstanceId, long amount, CancellationToken cancellationToken = default);
    Task<Phase2Result<ArtifactXpDto>> RemoveArtifactXpAsync(ulong discordUserId, ulong itemInstanceId, long amount, CancellationToken cancellationToken = default);
    Task<FinalStatsDto?> GetFinalStatsAsync(ulong discordUserId, ulong? characterId, string? characterName, CancellationToken cancellationToken = default);
}
