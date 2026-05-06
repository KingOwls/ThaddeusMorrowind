namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed record CharacterDetailDto(
    ulong Id,
    string Name,
    string? Nickname,
    uint Level,
    ulong Experience,
    string Nation,
    string Profession,
    string Role,
    string? NationBannerUrl,
    string? ProfessionBannerUrl,
    string? RoleBannerUrl,
    string? PortraitUrl,
    string? ThumbnailUrl,
    string? BannerUrl,
    bool IsMainCharacter,
    bool IsActiveCharacter,
    IReadOnlyList<CharacterStatDto> Stats);
