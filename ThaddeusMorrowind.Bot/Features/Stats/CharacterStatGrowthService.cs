using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;

namespace ThaddeusMorrowind.Bot.Features.Stats;

public sealed class CharacterStatGrowthService : ICharacterStatGrowthService
{
    private readonly GameDbContext _dbContext;

    public CharacterStatGrowthService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CharacterStatGrowthResult> RecalculateStatsAsync(
        ulong characterId,
        CancellationToken cancellationToken = default)
    {
        CharacterGrowthContext? context = await GetCharacterContextAsync(characterId, cancellationToken);

        if (context is null)
        {
            return Failure("No encontré un personaje activo con ese ID.");
        }

        await RebuildSkillStatModifiersAsync(context, cancellationToken);

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandTimeout = 120;

        command.CommandText = """
            INSERT INTO character_stats (
                character_id,
                stat_type_id,
                base_value,
                extra_value,
                updated_at
            )
            SELECT
                calculated_stats.character_id,
                calculated_stats.stat_type_id,
                calculated_stats.base_value,
                ROUND(
                    COALESCE(SUM(character_stat_modifiers.flat_value), 0)
                    + (
                        calculated_stats.base_value
                        * COALESCE(SUM(character_stat_modifiers.percent_value), 0)
                        / 100
                    ),
                    2
                ) AS extra_value,
                CURRENT_TIMESTAMP AS updated_at
            FROM (
                SELECT
                    @character_id AS character_id,
                    stat_types.id AS stat_type_id,
                    ROUND(
                        stat_types.default_base
                        + ((@character_level - 1) * COALESCE(SUM(stat_growth_rules.growth_per_level), 0))
                        + COALESCE(SUM(stat_growth_rules.flat_bonus), 0),
                        2
                    ) AS base_value
                FROM stat_types
                LEFT JOIN stat_growth_rules
                    ON stat_growth_rules.stat_type_id = stat_types.id
                   AND stat_growth_rules.is_active = TRUE
                   AND (
                        (stat_growth_rules.source_type = 'base'
                            AND stat_growth_rules.source_key = 'global')
                        OR (stat_growth_rules.source_type = 'nation'
                            AND stat_growth_rules.source_key = @nation_key)
                        OR (stat_growth_rules.source_type = 'role'
                            AND stat_growth_rules.source_key = @role_key)
                        OR (stat_growth_rules.source_type = 'profession'
                            AND stat_growth_rules.source_key = @profession_key)
                   )
                WHERE stat_types.is_active = TRUE
                  AND stat_types.can_be_base = TRUE
                GROUP BY
                    stat_types.id,
                    stat_types.default_base
            ) AS calculated_stats
            LEFT JOIN character_stat_modifiers
                ON character_stat_modifiers.character_id = calculated_stats.character_id
               AND character_stat_modifiers.stat_type_id = calculated_stats.stat_type_id
               AND character_stat_modifiers.is_active = TRUE
            GROUP BY
                calculated_stats.character_id,
                calculated_stats.stat_type_id,
                calculated_stats.base_value
            ON DUPLICATE KEY UPDATE
                base_value = VALUES(base_value),
                extra_value = VALUES(extra_value),
                updated_at = CURRENT_TIMESTAMP;
            """;

        AddParameter(command, "@character_id", context.CharacterId);
        AddParameter(command, "@character_level", context.Level);
        AddParameter(command, "@nation_key", context.NationKey);
        AddParameter(command, "@role_key", context.RoleKey);
        AddParameter(command, "@profession_key", context.ProfessionKey);

        await command.ExecuteNonQueryAsync(cancellationToken);

        await InsertGrowthLogAsync(
            context.CharacterId,
            context.Level,
            "stat_growth_recalculation",
            "Recalculo automático de estadísticas por nivel, nación, rol, profesión y habilidades equipadas.",
            cancellationToken);

        CharacterStatGrowthResult stats = await GetStatsAsync(characterId, cancellationToken);

        return stats with
        {
            Success = true,
            Message = $"Estadísticas recalculadas para **{context.CharacterName}** nivel {context.Level}."
        };
    }

    public async Task<CharacterStatGrowthResult> GetStatsAsync(
        ulong characterId,
        CancellationToken cancellationToken = default)
    {
        CharacterGrowthContext? context = await GetCharacterContextAsync(characterId, cancellationToken);

        if (context is null)
        {
            return Failure("No encontré un personaje activo con ese ID.");
        }

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandTimeout = 120;

        command.CommandText = """
            SELECT
                stat_types.stat_key,
                stat_types.name,
                stat_types.value_kind,
                character_stats.base_value,
                character_stats.extra_value,
                character_stats.base_value + character_stats.extra_value AS total_value
            FROM character_stats
            INNER JOIN stat_types
                ON stat_types.id = character_stats.stat_type_id
            WHERE character_stats.character_id = @character_id
              AND stat_types.is_active = TRUE
            ORDER BY stat_types.id;
            """;

        AddParameter(command, "@character_id", context.CharacterId);

        List<CharacterStatGrowthValueDto> stats = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(new CharacterStatGrowthValueDto(
                Convert.ToString(reader["stat_key"]) ?? string.Empty,
                Convert.ToString(reader["name"]) ?? string.Empty,
                Convert.ToString(reader["value_kind"]) ?? string.Empty,
                Convert.ToDecimal(reader["base_value"]),
                Convert.ToDecimal(reader["extra_value"]),
                Convert.ToDecimal(reader["total_value"])));
        }

        return new CharacterStatGrowthResult(
            true,
            $"Estadísticas actuales de **{context.CharacterName}**.",
            context.CharacterId,
            context.CharacterName,
            context.Level,
            context.NationKey,
            context.RoleKey,
            context.ProfessionKey,
            stats);
    }

    private async Task RebuildSkillStatModifiersAsync(
        CharacterGrowthContext context,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand deleteCommand = connection.CreateCommand();
        deleteCommand.CommandTimeout = 120;

        deleteCommand.CommandText = """
            DELETE FROM character_stat_modifiers
            WHERE character_id = @character_id
              AND source_type = 'skill';
            """;

        AddParameter(deleteCommand, "@character_id", context.CharacterId);

        await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

        await using DbCommand insertCommand = connection.CreateCommand();
        insertCommand.CommandTimeout = 120;

        insertCommand.CommandText = """
            INSERT INTO character_stat_modifiers (
                character_id,
                stat_type_id,
                source_type,
                source_id,
                source_key,
                flat_value,
                percent_value,
                description,
                is_active,
                created_at,
                updated_at
            )
            SELECT
                character_skill_loadout_slots.character_id,
                skill_stat_effects.stat_type_id,
                'skill' AS source_type,
                skill_templates.id AS source_id,
                skill_templates.skill_key AS source_key,
                skill_stat_effects.flat_value,
                skill_stat_effects.percent_value,
                CONCAT('Habilidad equipada: ', skill_templates.name) AS description,
                TRUE AS is_active,
                CURRENT_TIMESTAMP AS created_at,
                CURRENT_TIMESTAMP AS updated_at
            FROM character_skill_loadout_slots
            INNER JOIN skill_templates
                ON skill_templates.id = character_skill_loadout_slots.skill_template_id
            INNER JOIN skill_stat_effects
                ON skill_stat_effects.skill_template_id = skill_templates.id
               AND skill_stat_effects.is_active = TRUE
            WHERE character_skill_loadout_slots.character_id = @character_id
            ON DUPLICATE KEY UPDATE
                flat_value = VALUES(flat_value),
                percent_value = VALUES(percent_value),
                description = VALUES(description),
                is_active = TRUE,
                updated_at = CURRENT_TIMESTAMP;
            """;

        AddParameter(insertCommand, "@character_id", context.CharacterId);

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<CharacterGrowthContext?> GetCharacterContextAsync(
        ulong characterId,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandTimeout = 120;

        command.CommandText = """
            SELECT
                characters.id,
                characters.name,
                characters.level,
                nations.nation_key,
                roles.role_key,
                professions.profession_key
            FROM characters
            INNER JOIN nations
                ON nations.id = characters.nation_id
            INNER JOIN roles
                ON roles.id = characters.role_id
            INNER JOIN professions
                ON professions.id = characters.profession_id
            WHERE characters.id = @character_id
              AND characters.status = 'active'
            LIMIT 1;
            """;

        AddParameter(command, "@character_id", characterId);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterGrowthContext(
            Convert.ToUInt64(reader["id"]),
            Convert.ToString(reader["name"]) ?? string.Empty,
            Convert.ToUInt32(reader["level"]),
            Convert.ToString(reader["nation_key"]) ?? string.Empty,
            Convert.ToString(reader["role_key"]) ?? string.Empty,
            Convert.ToString(reader["profession_key"]) ?? string.Empty);
    }

    private async Task InsertGrowthLogAsync(
        ulong characterId,
        uint level,
        string sourceKey,
        string? reason,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandTimeout = 120;

        command.CommandText = """
            INSERT INTO character_stat_growth_logs (
                character_id,
                previous_level,
                new_level,
                source_key,
                reason,
                created_at
            )
            VALUES (
                @character_id,
                @previous_level,
                @new_level,
                @source_key,
                @reason,
                CURRENT_TIMESTAMP
            );
            """;

        AddParameter(command, "@character_id", characterId);
        AddParameter(command, "@previous_level", level);
        AddParameter(command, "@new_level", level);
        AddParameter(command, "@source_key", sourceKey);
        AddParameter(command, "@reason", reason);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static CharacterStatGrowthResult Failure(string message)
    {
        return new CharacterStatGrowthResult(
            false,
            message,
            null,
            null,
            null,
            null,
            null,
            null,
            Array.Empty<CharacterStatGrowthValueDto>());
    }

    private sealed record CharacterGrowthContext(
        ulong CharacterId,
        string CharacterName,
        uint Level,
        string NationKey,
        string RoleKey,
        string ProfessionKey);
}
