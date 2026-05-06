namespace ThaddeusMorrowind.Bot.Features.Skills;

public interface ISkillTreeService
{
    Task<SkillTreeDto?> GetTreeAsync(
        ulong discordUserId,
        ulong characterId,
        CancellationToken cancellationToken = default);

    Task<SkillListDto?> ListSkillsAsync(
        ulong discordUserId,
        ulong characterId,
        string? category,
        bool onlyKnown,
        bool allowAnyOwner = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillSlotDto>> GetAssignableSlotsAsync(
        ulong discordUserId,
        ulong characterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillSlotDto>> GetOccupiedSlotsAsync(
        ulong discordUserId,
        ulong characterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SkillTemplateOptionDto>> GetAvailableSkillsForSlotAsync(
        ulong discordUserId,
        ulong characterId,
        uint slotNumber,
        CancellationToken cancellationToken = default);

    Task<SkillLearnResult> GrantSkillAsync(
        ulong actorDiscordUserId,
        ulong characterId,
        ulong skillTemplateId,
        string sourceKey,
        string? reason,
        bool allowAnyOwner,
        CancellationToken cancellationToken = default);

    Task<SkillLearnResult> ForgetSkillAsync(
        ulong actorDiscordUserId,
        ulong characterId,
        ulong skillTemplateId,
        string sourceKey,
        string? reason,
        bool allowAnyOwner,
        CancellationToken cancellationToken = default);

    Task<SkillActionResult> AssignSkillAsync(
        ulong discordUserId,
        ulong characterId,
        uint slotNumber,
        ulong skillTemplateId,
        string actionKey,
        CancellationToken cancellationToken = default);

    Task<SkillActionResult> RemoveSkillAsync(
        ulong discordUserId,
        ulong characterId,
        uint slotNumber,
        CancellationToken cancellationToken = default);
}
