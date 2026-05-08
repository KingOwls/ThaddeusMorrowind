using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Characters;

public interface ICharacterService
{
    Task<CharacterCommandResult<CharacterProfileDto>> CreateAsync(
        ulong actorDiscordUserId,
        CharacterCreateRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CharacterCommandResult<IReadOnlyList<CharacterListItemDto>>> ListAsync(
        ulong actorDiscordUserId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<CharacterCommandResult<CharacterProfileDto>> GetAsync(
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<CharacterCommandResult<CharacterProfileDto>> SelectAsync(
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        CancellationToken cancellationToken = default);

    Task<CharacterCommandResult<CharacterProfileDto>> EditAsync(
        ulong actorDiscordUserId,
        CharacterEditRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CharacterCommandResult<CharacterProfileDto>> ArchiveAsync(
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        CancellationToken cancellationToken = default);

    Task<CharacterCommandResult<CharacterProfileDto>> RestoreAsync(
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        bool allowAnyOwner = false,
        CancellationToken cancellationToken = default);
}
