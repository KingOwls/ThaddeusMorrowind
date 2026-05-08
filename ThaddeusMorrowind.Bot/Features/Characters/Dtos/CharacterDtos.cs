namespace ThaddeusMorrowind.Bot.Features.Characters.Dtos;

public sealed record CharacterCreateRequestDto(
    string Name,
    string? Nickname,
    string? ImageUrl,
    string NationKeyOrName,
    string RoleKeyOrName,
    string ProfessionKeyOrName);

public sealed record CharacterEditRequestDto(
    ulong? CharacterId,
    string? CurrentName,
    string? NewName,
    string? NewNickname,
    string? NewImageUrl);

public sealed record CharacterLookupDto(
    ulong? CharacterId,
    string? CharacterName,
    ulong? TargetDiscordUserId);

public sealed record CharacterCommandResult<T>(
    bool Success,
    string Message,
    T? Data);

public sealed record CharacterListItemDto(
    ulong CharacterId,
    string Name,
    string? Nickname,
    string? ImageUrl,
    uint Level,
    ulong CurrentXp,
    ulong TotalXp,
    string NationName,
    string RoleName,
    string ProfessionName,
    string CharacterStatus,
    bool IsActiveCharacter,
    DateTime CreatedAt);

public sealed record CharacterStatValueDto(
    string StatKey,
    string Name,
    string ValueKind,
    decimal BaseValue,
    decimal ExtraValue,
    decimal TotalValue);

public sealed record CharacterEquipmentSummaryDto(
    string WeaponName,
    string ArtifactSummary,
    string UniqueArtifactName);

public sealed record CharacterSkillSlotDto(
    uint SlotNumber,
    string SlotCategory,
    uint UnlockLevel,
    string SlotName,
    bool IsUnlocked,
    ulong? SkillTemplateId,
    string? SkillName,
    string? SkillCategory);

public sealed record CharacterSkillTreeSummaryDto(
    uint TotalSlots,
    uint UnlockedSlots,
    uint EquippedSlots,
    IReadOnlyList<CharacterSkillSlotDto> Slots);

public sealed record CharacterProfileDto(
    ulong CharacterId,
    ulong UserAccountId,
    ulong OwnerDiscordUserId,
    string OwnerUsername,
    string Name,
    string? Nickname,
    string? ImageUrl,
    uint Level,
    ulong CurrentXp,
    ulong TotalXp,
    string NationKey,
    string NationName,
    string? NationIconUrl,
    string? NationBannerUrl,
    string RoleKey,
    string RoleName,
    string? RoleIconUrl,
    string? RoleBannerUrl,
    string ProfessionKey,
    string ProfessionName,
    string? ProfessionIconUrl,
    string? ProfessionBannerUrl,
    bool IsActiveCharacter,
    string CharacterStatus,
    DateTime CreatedAt,
    CharacterEquipmentSummaryDto Equipment,
    CharacterSkillTreeSummaryDto SkillTree,
    IReadOnlyList<CharacterStatValueDto> Stats);
