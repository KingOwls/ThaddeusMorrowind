namespace ThaddeusMorrowind.Bot.Features.Stats;

public interface ICharacterStatGrowthService
{
    Task<CharacterStatGrowthResult> RecalculateStatsAsync(
        ulong characterId,
        CancellationToken cancellationToken = default);

    Task<CharacterStatGrowthResult> GetStatsAsync(
        ulong characterId,
        CancellationToken cancellationToken = default);
}
