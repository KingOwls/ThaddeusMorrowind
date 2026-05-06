namespace ThaddeusMorrowind.Bot.Features.Skills;

public sealed record SkillTreeDto(
    ulong CharacterId,
    string CharacterName,
    uint Level,
    IReadOnlyList<SkillSlotDto> Slots);

public sealed record SkillSlotDto(
    uint SlotNumber,
    string SlotType,
    uint UnlockLevel,
    string SlotName,
    bool IsUnlocked,
    ulong? SkillTemplateId,
    string? SkillName,
    string? SkillShortDescription,
    string? SkillIconUrl);

public sealed record SkillTemplateOptionDto(
    ulong Id,
    string SkillKey,
    string Name,
    string ShortDescription,
    string Description,
    string SkillCategory,
    string OriginType,
    string? OriginKey,
    uint RequiredLevel,
    uint ManaCost,
    uint CooldownTurns,
    string? IconUrl,
    bool IsKnown);

public sealed record SkillListDto(
    ulong CharacterId,
    string CharacterName,
    uint Level,
    bool OnlyKnown,
    string? CategoryFilter,
    IReadOnlyList<SkillTemplateOptionDto> Skills);

public sealed record SkillActionResult(
    bool Success,
    string Message,
    SkillTreeDto? Tree);

public sealed record SkillLearnResult(
    bool Success,
    string Message,
    SkillListDto? SkillList);
