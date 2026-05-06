namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed record CharacterCatalogOptionDto(
    ulong Id,
    string Key,
    string Name,
    string? ShortDescription,
    string? Description,
    string? IconUrl,
    string? BannerUrl,
    uint DisplayOrder);
