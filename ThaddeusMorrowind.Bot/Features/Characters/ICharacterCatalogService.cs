using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Characters;

public interface ICharacterCatalogService
{
    Task<IReadOnlyList<CharacterCatalogOptionDto>> GetNationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CharacterCatalogOptionDto>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CharacterCatalogOptionDto>> GetProfessionsAsync(
        CancellationToken cancellationToken = default);

    Task<CharacterCatalogOptionDto?> GetByKeyAsync(
        string catalogType,
        string key,
        CancellationToken cancellationToken = default);
}
