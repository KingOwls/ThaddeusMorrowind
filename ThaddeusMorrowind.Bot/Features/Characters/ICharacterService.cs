namespace ThaddeusMorrowind.Bot.Features.Characters;

public interface ICharacterService
{
    Task<IReadOnlyList<CharacterCatalogOptionDto>> GetNationOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CharacterCatalogOptionDto>> GetProfessionOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CharacterCatalogOptionDto>> GetRoleOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<CharacterCatalogOptionDto?> GetNationOptionAsync(
        ulong id,
        CancellationToken cancellationToken = default);

    Task<CharacterCatalogOptionDto?> GetProfessionOptionAsync(
        ulong id,
        CancellationToken cancellationToken = default);

    Task<CharacterCatalogOptionDto?> GetRoleOptionAsync(
        ulong id,
        CancellationToken cancellationToken = default);

    Task<CharacterCreateResult> CreateCharacterFromCatalogAsync(
        ulong discordUserId,
        string username,
        string? displayName,
        string characterName,
        string? nickname,
        ulong nationId,
        ulong professionId,
        ulong roleId,
        string? portraitUrl = null,
        CancellationToken cancellationToken = default);

    Task<CharacterCreateResult> CreateCharacterAsync(
        ulong discordUserId,
        string username,
        string? displayName,
        string characterName,
        string nationInput,
        string professionInput,
        string roleInput,
        CancellationToken cancellationToken = default);

    Task<CharacterCreateResult> UpdateCharacterAsync(
        ulong discordUserId,
        ulong characterId,
        string? newName,
        string? newNickname,
        string? newPortraitUrl,
        bool clearPortrait = false,
        CancellationToken cancellationToken = default);

    Task<CharacterDeleteResult> AdminDeleteCharacterAsync(
        ulong characterId,
        ulong adminDiscordUserId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CharacterSummaryDto>> ListCharactersAsync(
        ulong discordUserId,
        CancellationToken cancellationToken = default);

    Task<CharacterDetailDto?> GetCharacterAsync(
        ulong discordUserId,
        ulong? characterId,
        CancellationToken cancellationToken = default);

    Task<CharacterDetailDto?> SelectCharacterAsync(
        ulong discordUserId,
        ulong characterId,
        CancellationToken cancellationToken = default);
}
