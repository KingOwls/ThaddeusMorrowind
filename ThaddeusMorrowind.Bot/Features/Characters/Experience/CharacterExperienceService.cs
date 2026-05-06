using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;
using ThaddeusMorrowind.Bot.Data.Entities;
using ThaddeusMorrowind.Bot.Features.Stats;

namespace ThaddeusMorrowind.Bot.Features.Characters.Experience;

public sealed class CharacterExperienceService : ICharacterExperienceService
{
    private readonly GameDbContext _dbContext;
    private readonly ICharacterStatGrowthService _statGrowthService;

    public CharacterExperienceService(
        GameDbContext dbContext,
        ICharacterStatGrowthService statGrowthService)
    {
        _dbContext = dbContext;
        _statGrowthService = statGrowthService;
    }

    public async Task<CharacterExperienceResult> AdjustExperienceAsync(
        ulong characterId,
        long deltaExperience,
        ulong actorDiscordUserId,
        string sourceKey,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (deltaExperience == 0)
        {
            return Failure("La cantidad de experiencia no puede ser 0.");
        }

        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(
                x => x.Id == characterId
                     && x.Status == "active",
                cancellationToken);

        if (character is null)
        {
            return Failure("No encontré un personaje activo con ese ID.");
        }

        ExperienceCurve? activeCurve = await _dbContext.ExperienceCurves
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive, cancellationToken);

        if (activeCurve is null)
        {
            return Failure("No hay una curva de experiencia activa.");
        }

        ulong previousExperience = character.Experience;
        uint previousLevel = character.Level;

        ulong maxExperience = await GetMaxExperienceAsync(activeCurve.Id, cancellationToken);
        ulong newExperience = ApplyDelta(previousExperience, deltaExperience, maxExperience);
        uint newLevel = await CalculateLevelAsync(activeCurve.Id, newExperience, cancellationToken);

        character.Experience = newExperience;
        character.Level = newLevel;
        character.UpdatedAt = DateTime.UtcNow;

        _dbContext.CharacterExperienceLogs.Add(new CharacterExperienceLog
        {
            CharacterId = character.Id,
            UserAccountId = character.UserAccountId,
            ActorDiscordUserId = actorDiscordUserId.ToString(),
            SourceKey = string.IsNullOrWhiteSpace(sourceKey) ? "manual_admin" : sourceKey.Trim(),
            DeltaExperience = deltaExperience,
            PreviousExperience = previousExperience,
            NewExperience = newExperience,
            PreviousLevel = previousLevel,
            NewLevel = newLevel,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _statGrowthService.RecalculateStatsAsync(character.Id, cancellationToken);

        ExperienceLevel currentLevelInfo = await _dbContext.ExperienceLevels
            .AsNoTracking()
            .Where(x => x.ExperienceCurveId == activeCurve.Id && x.Level == newLevel)
            .FirstAsync(cancellationToken);

        ExperienceLevel? nextLevelInfo = await _dbContext.ExperienceLevels
            .AsNoTracking()
            .Where(x => x.ExperienceCurveId == activeCurve.Id && x.Level == newLevel + 1)
            .FirstOrDefaultAsync(cancellationToken);

        ulong xpForCurrent = currentLevelInfo.TotalXpRequired;
        ulong xpForNext = nextLevelInfo?.TotalXpRequired ?? currentLevelInfo.TotalXpRequired;
        ulong progress = newExperience > xpForCurrent ? newExperience - xpForCurrent : 0;
        ulong needed = nextLevelInfo is null || newExperience >= xpForNext ? 0 : xpForNext - newExperience;

        string levelText = newLevel > previousLevel
            ? $" Subió de nivel {previousLevel} → {newLevel}."
            : newLevel < previousLevel
                ? $" Bajó de nivel {previousLevel} → {newLevel}."
                : $" Permanece en nivel {newLevel}.";

        return new CharacterExperienceResult(
            true,
            $"Experiencia actualizada para {character.Name}.{levelText}",
            character.Id,
            character.Name,
            deltaExperience,
            previousExperience,
            newExperience,
            previousLevel,
            newLevel,
            xpForCurrent,
            xpForNext,
            progress,
            needed);
    }

    private static CharacterExperienceResult Failure(string message)
    {
        return new CharacterExperienceResult(
            false,
            message,
            null,
            null,
            0,
            0,
            0,
            1,
            1,
            0,
            0,
            0,
            0);
    }

    private async Task<uint> CalculateLevelAsync(
        ulong curveId,
        ulong totalExperience,
        CancellationToken cancellationToken)
    {
        uint? level = await _dbContext.ExperienceLevels
            .AsNoTracking()
            .Where(x => x.ExperienceCurveId == curveId && x.TotalXpRequired <= totalExperience)
            .OrderByDescending(x => x.Level)
            .Select(x => (uint?)x.Level)
            .FirstOrDefaultAsync(cancellationToken);

        return level ?? 1;
    }

    private async Task<ulong> GetMaxExperienceAsync(
        ulong curveId,
        CancellationToken cancellationToken)
    {
        ulong? maxExperience = await _dbContext.ExperienceLevels
            .AsNoTracking()
            .Where(x => x.ExperienceCurveId == curveId)
            .OrderByDescending(x => x.Level)
            .Select(x => (ulong?)x.TotalXpRequired)
            .FirstOrDefaultAsync(cancellationToken);

        return maxExperience ?? 0;
    }

    private static ulong ApplyDelta(
        ulong previousExperience,
        long deltaExperience,
        ulong maxExperience)
    {
        if (deltaExperience > 0)
        {
            ulong added = (ulong)deltaExperience;

            if (previousExperience > maxExperience - Math.Min(added, maxExperience))
            {
                return maxExperience;
            }

            ulong result = previousExperience + added;
            return result > maxExperience ? maxExperience : result;
        }

        ulong removed = (ulong)Math.Abs(deltaExperience);

        if (removed >= previousExperience)
        {
            return 0;
        }

        return previousExperience - removed;
    }
}
