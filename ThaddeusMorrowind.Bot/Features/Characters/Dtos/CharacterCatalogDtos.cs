namespace ThaddeusMorrowind.Bot.Features.Characters.Dtos;

public sealed record CharacterCatalogOptionDto(
    string CatalogType,
    ulong Id,
    string Key,
    string Name,
    string? ShortDescription,
    string? Description,
    string? IconUrl,
    string? BannerUrl,
    uint DisplayOrder);
