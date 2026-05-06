using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;
using ThaddeusMorrowind.Bot.Features.Stats;

namespace ThaddeusMorrowind.Bot.Features.Skills;

public sealed class SkillTreeService : ISkillTreeService
{
    private readonly GameDbContext _dbContext;
    private readonly ICharacterStatGrowthService _statGrowthService;

    public SkillTreeService(
        GameDbContext dbContext,
        ICharacterStatGrowthService statGrowthService)
    {
        _dbContext = dbContext;
        _statGrowthService = statGrowthService;
    }

    public async Task<SkillTreeDto?> GetTreeAsync(
        ulong discordUserId,
        ulong characterId,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSlotsAsync(cancellationToken);

        CharacterContext? context = await GetCharacterContextAsync(
            discordUserId,
            characterId,
            allowAnyOwner: false,
            cancellationToken);

        if (context is null)
        {
            return null;
        }

        IReadOnlyList<SkillSlotDto> slots = await LoadSlotsAsync(context, cancellationToken);

        return new SkillTreeDto(
            context.CharacterId,
            context.CharacterName,
            context.Level,
            slots);
    }

    public async Task<SkillListDto?> ListSkillsAsync(
        ulong discordUserId,
        ulong characterId,
        string? category,
        bool onlyKnown,
        bool allowAnyOwner = false,
        CancellationToken cancellationToken = default)
    {
        CharacterContext? context = await GetCharacterContextAsync(
            discordUserId,
            characterId,
            allowAnyOwner,
            cancellationToken);

        if (context is null)
        {
            return null;
        }

        string? normalizedCategory = NormalizeCategory(category);

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();

        string categoryFilterSql = normalizedCategory is null
            ? ""
            : "AND st.skill_category = @category";

        string knownFilterSql = onlyKnown
            ? """
              AND EXISTS (
                    SELECT 1
                    FROM character_skill_unlocks AS cu
                    WHERE cu.character_id = @character_id
                      AND cu.skill_template_id = st.id
                  )
              """
            : "";

        command.CommandText = $"""
            SELECT
                st.id,
                st.skill_key,
                st.name,
                COALESCE(st.short_description, '') AS short_description,
                COALESCE(st.description, '') AS description,
                st.skill_category,
                st.origin_type,
                st.origin_key,
                st.required_level,
                st.mana_cost,
                st.cooldown_turns,
                st.icon_url,
                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM character_skill_unlocks AS cu
                        WHERE cu.character_id = @character_id
                          AND cu.skill_template_id = st.id
                    ) THEN 1
                    ELSE 0
                END AS is_known
            FROM skill_templates AS st
            WHERE st.is_active = TRUE
              AND st.required_level <= @character_level
              AND (
                    st.origin_type = 'global'
                    OR (st.origin_type = 'nation' AND st.origin_key = @nation_key)
                    OR (st.origin_type = 'profession' AND st.origin_key = @profession_key)
                    OR (st.origin_type = 'role' AND st.origin_key = @role_key)
                  )
              {categoryFilterSql}
              {knownFilterSql}
            ORDER BY
                is_known DESC,
                st.skill_category,
                st.origin_type,
                st.display_order,
                st.name
            LIMIT 50;
            """;

        AddParameter(command, "@character_id", context.CharacterId);
        AddParameter(command, "@character_level", context.Level);
        AddParameter(command, "@nation_key", context.NationKey);
        AddParameter(command, "@profession_key", context.ProfessionKey);
        AddParameter(command, "@role_key", context.RoleKey);

        if (normalizedCategory is not null)
        {
            AddParameter(command, "@category", normalizedCategory);
        }

        List<SkillTemplateOptionDto> skills = await ReadSkillOptionsAsync(command, cancellationToken);

        return new SkillListDto(
            context.CharacterId,
            context.CharacterName,
            context.Level,
            onlyKnown,
            normalizedCategory,
            skills);
    }

    public async Task<IReadOnlyList<SkillSlotDto>> GetAssignableSlotsAsync(
        ulong discordUserId,
        ulong characterId,
        CancellationToken cancellationToken = default)
    {
        await _statGrowthService.RecalculateStatsAsync(characterId, cancellationToken);

        SkillTreeDto? tree = await GetTreeAsync(discordUserId, characterId, cancellationToken);

        if (tree is null)
        {
            return Array.Empty<SkillSlotDto>();
        }

        return tree.Slots
            .Where(x => x.IsUnlocked)
            .OrderBy(x => x.SlotNumber)
            .ToList();
    }

    public async Task<IReadOnlyList<SkillSlotDto>> GetOccupiedSlotsAsync(
        ulong discordUserId,
        ulong characterId,
        CancellationToken cancellationToken = default)
    {
        SkillTreeDto? tree = await GetTreeAsync(discordUserId, characterId, cancellationToken);

        if (tree is null)
        {
            return Array.Empty<SkillSlotDto>();
        }

        return tree.Slots
            .Where(x => x.IsUnlocked && x.SkillTemplateId is not null)
            .OrderBy(x => x.SlotNumber)
            .ToList();
    }

    public async Task<IReadOnlyList<SkillTemplateOptionDto>> GetAvailableSkillsForSlotAsync(
        ulong discordUserId,
        ulong characterId,
        uint slotNumber,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSlotsAsync(cancellationToken);

        CharacterContext? context = await GetCharacterContextAsync(
            discordUserId,
            characterId,
            allowAnyOwner: false,
            cancellationToken);

        if (context is null)
        {
            return Array.Empty<SkillTemplateOptionDto>();
        }

        SkillSlotDto? slot = (await LoadSlotsAsync(context, cancellationToken))
            .FirstOrDefault(x => x.SlotNumber == slotNumber);

        if (slot is null || !slot.IsUnlocked)
        {
            return Array.Empty<SkillTemplateOptionDto>();
        }

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                st.id,
                st.skill_key,
                st.name,
                COALESCE(st.short_description, '') AS short_description,
                COALESCE(st.description, '') AS description,
                st.skill_category,
                st.origin_type,
                st.origin_key,
                st.required_level,
                st.mana_cost,
                st.cooldown_turns,
                st.icon_url,
                1 AS is_known
            FROM skill_templates AS st
            INNER JOIN character_skill_unlocks AS cu
                ON cu.skill_template_id = st.id
               AND cu.character_id = @character_id
            WHERE st.is_active = TRUE
              AND st.skill_category = @slot_type
              AND st.required_level <= @character_level
              AND (
                    st.origin_type = 'global'
                    OR (st.origin_type = 'nation' AND st.origin_key = @nation_key)
                    OR (st.origin_type = 'profession' AND st.origin_key = @profession_key)
                    OR (st.origin_type = 'role' AND st.origin_key = @role_key)
                  )
              AND NOT EXISTS (
                    SELECT 1
                    FROM character_skill_loadout_slots AS cs
                    WHERE cs.character_id = @character_id
                      AND cs.skill_template_id = st.id
                      AND cs.slot_number <> @slot_number
                  )
            ORDER BY st.display_order, st.name
            LIMIT 25;
            """;

        AddParameter(command, "@slot_type", slot.SlotType);
        AddParameter(command, "@character_level", context.Level);
        AddParameter(command, "@nation_key", context.NationKey);
        AddParameter(command, "@profession_key", context.ProfessionKey);
        AddParameter(command, "@role_key", context.RoleKey);
        AddParameter(command, "@character_id", context.CharacterId);
        AddParameter(command, "@slot_number", slotNumber);

        return await ReadSkillOptionsAsync(command, cancellationToken);
    }

    public async Task<SkillLearnResult> GrantSkillAsync(
        ulong actorDiscordUserId,
        ulong characterId,
        ulong skillTemplateId,
        string sourceKey,
        string? reason,
        bool allowAnyOwner,
        CancellationToken cancellationToken = default)
    {
        CharacterContext? context = await GetCharacterContextAsync(
            actorDiscordUserId,
            characterId,
            allowAnyOwner,
            cancellationToken);

        if (context is null)
        {
            return new SkillLearnResult(false, "No encontré ese personaje o no tienes permisos sobre él.", null);
        }

        SkillTemplateOptionDto? skill = await GetCompatibleSkillByIdAsync(context, skillTemplateId, cancellationToken);

        if (skill is null)
        {
            SkillListDto? list = await ListSkillsAsync(actorDiscordUserId, characterId, null, false, allowAnyOwner, cancellationToken);
            return new SkillLearnResult(false, "La habilidad no existe, no está activa o no es compatible con el personaje por nivel/origen.", list);
        }

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO character_skill_unlocks (
                character_id,
                skill_template_id,
                source_key,
                learned_at,
                learned_by_discord_user_id,
                reason
            )
            VALUES (
                @character_id,
                @skill_template_id,
                @source_key,
                CURRENT_TIMESTAMP,
                @actor_discord_user_id,
                @reason
            )
            ON DUPLICATE KEY UPDATE
                source_key = VALUES(source_key),
                learned_by_discord_user_id = VALUES(learned_by_discord_user_id),
                reason = VALUES(reason);
            """;

        AddParameter(command, "@character_id", context.CharacterId);
        AddParameter(command, "@skill_template_id", skillTemplateId);
        AddParameter(command, "@source_key", sourceKey);
        AddParameter(command, "@actor_discord_user_id", actorDiscordUserId.ToString());
        AddParameter(command, "@reason", reason);

        await command.ExecuteNonQueryAsync(cancellationToken);

        await InsertUnlockLogAsync(
            context,
            actorDiscordUserId,
            skillTemplateId,
            "grant",
            sourceKey,
            reason,
            cancellationToken);

        SkillListDto? updatedList = await ListSkillsAsync(
            actorDiscordUserId,
            characterId,
            skill.SkillCategory,
            true,
            allowAnyOwner,
            cancellationToken);

        return new SkillLearnResult(
            true,
            $"**{context.CharacterName}** obtuvo la habilidad **{skill.Name}**.",
            updatedList);
    }

    public async Task<SkillLearnResult> ForgetSkillAsync(
        ulong actorDiscordUserId,
        ulong characterId,
        ulong skillTemplateId,
        string sourceKey,
        string? reason,
        bool allowAnyOwner,
        CancellationToken cancellationToken = default)
    {
        CharacterContext? context = await GetCharacterContextAsync(
            actorDiscordUserId,
            characterId,
            allowAnyOwner,
            cancellationToken);

        if (context is null)
        {
            return new SkillLearnResult(false, "No encontré ese personaje o no tienes permisos sobre él.", null);
        }

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        string? skillName = await GetSkillNameAsync(skillTemplateId, cancellationToken);

        if (skillName is null)
        {
            return new SkillLearnResult(false, "No encontré esa habilidad.", null);
        }

        await using DbCommand deleteLoadoutCommand = connection.CreateCommand();
        deleteLoadoutCommand.CommandText = """
            DELETE FROM character_skill_loadout_slots
            WHERE character_id = @character_id
              AND skill_template_id = @skill_template_id;
            """;
        AddParameter(deleteLoadoutCommand, "@character_id", context.CharacterId);
        AddParameter(deleteLoadoutCommand, "@skill_template_id", skillTemplateId);
        await deleteLoadoutCommand.ExecuteNonQueryAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM character_skill_unlocks
            WHERE character_id = @character_id
              AND skill_template_id = @skill_template_id;
            """;
        AddParameter(command, "@character_id", context.CharacterId);
        AddParameter(command, "@skill_template_id", skillTemplateId);

        int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affectedRows <= 0)
        {
            SkillListDto? list = await ListSkillsAsync(actorDiscordUserId, characterId, null, true, allowAnyOwner, cancellationToken);
            return new SkillLearnResult(false, $"El personaje no tenía aprendida la habilidad **{skillName}**.", list);
        }

        await InsertUnlockLogAsync(
            context,
            actorDiscordUserId,
            skillTemplateId,
            "forget",
            sourceKey,
            reason,
            cancellationToken);

        await _statGrowthService.RecalculateStatsAsync(characterId, cancellationToken);

        SkillListDto? updatedList = await ListSkillsAsync(
            actorDiscordUserId,
            characterId,
            null,
            true,
            allowAnyOwner,
            cancellationToken);

        return new SkillLearnResult(
            true,
            $"**{context.CharacterName}** olvidó la habilidad **{skillName}**. Si estaba equipada, fue retirada del árbol.",
            updatedList);
    }

    public async Task<SkillActionResult> AssignSkillAsync(
        ulong discordUserId,
        ulong characterId,
        uint slotNumber,
        ulong skillTemplateId,
        string actionKey,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSlotsAsync(cancellationToken);

        CharacterContext? context = await GetCharacterContextAsync(
            discordUserId,
            characterId,
            allowAnyOwner: false,
            cancellationToken);

        if (context is null)
        {
            return new SkillActionResult(false, "No encontré ese personaje entre tus personajes activos.", null);
        }

        SkillSlotDto? slot = (await LoadSlotsAsync(context, cancellationToken))
            .FirstOrDefault(x => x.SlotNumber == slotNumber);

        if (slot is null)
        {
            return new SkillActionResult(false, "Ese espacio de habilidad no existe.", await GetTreeAsync(discordUserId, characterId, cancellationToken));
        }

        if (!slot.IsUnlocked)
        {
            return new SkillActionResult(false, $"Ese espacio se desbloquea en nivel {slot.UnlockLevel}.", await GetTreeAsync(discordUserId, characterId, cancellationToken));
        }

        IReadOnlyList<SkillTemplateOptionDto> available = await GetAvailableSkillsForSlotAsync(
            discordUserId,
            characterId,
            slotNumber,
            cancellationToken);

        SkillTemplateOptionDto? selectedSkill = available.FirstOrDefault(x => x.Id == skillTemplateId);

        if (selectedSkill is null)
        {
            return new SkillActionResult(
                false,
                "La habilidad no está aprendida, no es compatible con ese espacio, no cumple nivel u origen, o ya está equipada en otro espacio.",
                await GetTreeAsync(discordUserId, characterId, cancellationToken));
        }

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand previousCommand = connection.CreateCommand();
        previousCommand.CommandText = """
            SELECT skill_template_id
            FROM character_skill_loadout_slots
            WHERE character_id = @character_id
              AND slot_number = @slot_number
            LIMIT 1;
            """;
        AddParameter(previousCommand, "@character_id", characterId);
        AddParameter(previousCommand, "@slot_number", slotNumber);

        object? previousRaw = await previousCommand.ExecuteScalarAsync(cancellationToken);
        ulong? previousSkillId = previousRaw is null || previousRaw is DBNull
            ? null
            : Convert.ToUInt64(previousRaw);

        string finalAction = previousSkillId is null ? "place" : "replace";

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO character_skill_loadout_slots (
                character_id,
                slot_number,
                skill_template_id,
                assigned_at,
                updated_at
            )
            VALUES (
                @character_id,
                @slot_number,
                @skill_template_id,
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            )
            ON DUPLICATE KEY UPDATE
                skill_template_id = VALUES(skill_template_id),
                updated_at = CURRENT_TIMESTAMP;
            """;
        AddParameter(command, "@character_id", characterId);
        AddParameter(command, "@slot_number", slotNumber);
        AddParameter(command, "@skill_template_id", skillTemplateId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        await InsertLoadoutLogAsync(
            context,
            discordUserId,
            finalAction,
            slotNumber,
            previousSkillId,
            skillTemplateId,
            actionKey,
            cancellationToken);

        SkillTreeDto? tree = await GetTreeAsync(discordUserId, characterId, cancellationToken);

        return new SkillActionResult(
            true,
            previousSkillId is null
                ? $"Habilidad **{selectedSkill.Name}** colocada en el espacio {slotNumber}."
                : $"Habilidad reemplazada por **{selectedSkill.Name}** en el espacio {slotNumber}.",
            tree);
    }

    public async Task<SkillActionResult> RemoveSkillAsync(
        ulong discordUserId,
        ulong characterId,
        uint slotNumber,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSlotsAsync(cancellationToken);

        CharacterContext? context = await GetCharacterContextAsync(
            discordUserId,
            characterId,
            allowAnyOwner: false,
            cancellationToken);

        if (context is null)
        {
            return new SkillActionResult(false, "No encontré ese personaje entre tus personajes activos.", null);
        }

        SkillTreeDto? beforeTree = await GetTreeAsync(discordUserId, characterId, cancellationToken);
        SkillSlotDto? slot = beforeTree?.Slots.FirstOrDefault(x => x.SlotNumber == slotNumber);

        if (slot is null)
        {
            return new SkillActionResult(false, "Ese espacio de habilidad no existe.", beforeTree);
        }

        if (slot.SkillTemplateId is null)
        {
            return new SkillActionResult(false, "Ese espacio ya está vacío.", beforeTree);
        }

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM character_skill_loadout_slots
            WHERE character_id = @character_id
              AND slot_number = @slot_number;
            """;
        AddParameter(command, "@character_id", characterId);
        AddParameter(command, "@slot_number", slotNumber);

        await command.ExecuteNonQueryAsync(cancellationToken);

        await InsertLoadoutLogAsync(
            context,
            discordUserId,
            "remove",
            slotNumber,
            slot.SkillTemplateId,
            null,
            "manual_remove",
            cancellationToken);

        await _statGrowthService.RecalculateStatsAsync(characterId, cancellationToken);

        SkillTreeDto? tree = await GetTreeAsync(discordUserId, characterId, cancellationToken);

        return new SkillActionResult(true, $"Se quitó la habilidad del espacio {slotNumber}.", tree);
    }

    private async Task<IReadOnlyList<SkillSlotDto>> LoadSlotsAsync(
        CharacterContext context,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                su.slot_number,
                su.slot_type,
                su.unlock_level,
                su.name AS slot_name,
                cs.skill_template_id,
                st.name AS skill_name,
                st.short_description AS skill_short_description,
                st.icon_url AS skill_icon_url
            FROM skill_slot_unlocks AS su
            LEFT JOIN character_skill_loadout_slots AS cs
                ON cs.slot_number = su.slot_number
               AND cs.character_id = @character_id
            LEFT JOIN skill_templates AS st
                ON st.id = cs.skill_template_id
            ORDER BY su.slot_number;
            """;
        AddParameter(command, "@character_id", context.CharacterId);

        List<SkillSlotDto> result = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            uint unlockLevel = Convert.ToUInt32(reader["unlock_level"]);

            result.Add(new SkillSlotDto(
                Convert.ToUInt32(reader["slot_number"]),
                Convert.ToString(reader["slot_type"]) ?? string.Empty,
                unlockLevel,
                Convert.ToString(reader["slot_name"]) ?? string.Empty,
                context.Level >= unlockLevel,
                reader["skill_template_id"] is DBNull ? null : Convert.ToUInt64(reader["skill_template_id"]),
                reader["skill_name"] is DBNull ? null : Convert.ToString(reader["skill_name"]),
                reader["skill_short_description"] is DBNull ? null : Convert.ToString(reader["skill_short_description"]),
                reader["skill_icon_url"] is DBNull ? null : Convert.ToString(reader["skill_icon_url"])));
        }

        return result;
    }

    private async Task<CharacterContext?> GetCharacterContextAsync(
        ulong discordUserId,
        ulong characterId,
        bool allowAnyOwner,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();

        string ownerFilter = allowAnyOwner
            ? ""
            : "AND u.discord_user_id = @discord_user_id";

        command.CommandText = $"""
            SELECT
                c.id,
                c.name,
                c.level,
                c.user_account_id,
                n.nation_key,
                p.profession_key,
                r.role_key
            FROM characters AS c
            INNER JOIN user_accounts AS u
                ON u.id = c.user_account_id
            INNER JOIN nations AS n
                ON n.id = c.nation_id
            INNER JOIN professions AS p
                ON p.id = c.profession_id
            INNER JOIN roles AS r
                ON r.id = c.role_id
            WHERE c.id = @character_id
              AND c.status = 'active'
              {ownerFilter}
            LIMIT 1;
            """;

        AddParameter(command, "@character_id", characterId);

        if (!allowAnyOwner)
        {
            AddParameter(command, "@discord_user_id", discordUserId.ToString());
        }

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterContext(
            Convert.ToUInt64(reader["id"]),
            Convert.ToString(reader["name"]) ?? string.Empty,
            Convert.ToUInt32(reader["level"]),
            Convert.ToUInt64(reader["user_account_id"]),
            Convert.ToString(reader["nation_key"]) ?? string.Empty,
            Convert.ToString(reader["profession_key"]) ?? string.Empty,
            Convert.ToString(reader["role_key"]) ?? string.Empty);
    }

    private async Task<SkillTemplateOptionDto?> GetCompatibleSkillByIdAsync(
        CharacterContext context,
        ulong skillTemplateId,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                st.id,
                st.skill_key,
                st.name,
                COALESCE(st.short_description, '') AS short_description,
                COALESCE(st.description, '') AS description,
                st.skill_category,
                st.origin_type,
                st.origin_key,
                st.required_level,
                st.mana_cost,
                st.cooldown_turns,
                st.icon_url,
                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM character_skill_unlocks AS cu
                        WHERE cu.character_id = @character_id
                          AND cu.skill_template_id = st.id
                    ) THEN 1
                    ELSE 0
                END AS is_known
            FROM skill_templates AS st
            WHERE st.id = @skill_template_id
              AND st.is_active = TRUE
              AND st.required_level <= @character_level
              AND (
                    st.origin_type = 'global'
                    OR (st.origin_type = 'nation' AND st.origin_key = @nation_key)
                    OR (st.origin_type = 'profession' AND st.origin_key = @profession_key)
                    OR (st.origin_type = 'role' AND st.origin_key = @role_key)
                  )
            LIMIT 1;
            """;

        AddParameter(command, "@skill_template_id", skillTemplateId);
        AddParameter(command, "@character_id", context.CharacterId);
        AddParameter(command, "@character_level", context.Level);
        AddParameter(command, "@nation_key", context.NationKey);
        AddParameter(command, "@profession_key", context.ProfessionKey);
        AddParameter(command, "@role_key", context.RoleKey);

        List<SkillTemplateOptionDto> skills = await ReadSkillOptionsAsync(command, cancellationToken);

        return skills.FirstOrDefault();
    }

    private async Task<List<SkillTemplateOptionDto>> ReadSkillOptionsAsync(
        DbCommand command,
        CancellationToken cancellationToken)
    {
        List<SkillTemplateOptionDto> result = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SkillTemplateOptionDto(
                Convert.ToUInt64(reader["id"]),
                Convert.ToString(reader["skill_key"]) ?? string.Empty,
                Convert.ToString(reader["name"]) ?? string.Empty,
                Convert.ToString(reader["short_description"]) ?? string.Empty,
                Convert.ToString(reader["description"]) ?? string.Empty,
                Convert.ToString(reader["skill_category"]) ?? string.Empty,
                Convert.ToString(reader["origin_type"]) ?? string.Empty,
                reader["origin_key"] is DBNull ? null : Convert.ToString(reader["origin_key"]),
                Convert.ToUInt32(reader["required_level"]),
                Convert.ToUInt32(reader["mana_cost"]),
                Convert.ToUInt32(reader["cooldown_turns"]),
                reader["icon_url"] is DBNull ? null : Convert.ToString(reader["icon_url"]),
                Convert.ToInt32(reader["is_known"]) == 1));
        }

        return result;
    }

    private async Task<string?> GetSkillNameAsync(
        ulong skillTemplateId,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM skill_templates WHERE id = @skill_template_id LIMIT 1;";
        AddParameter(command, "@skill_template_id", skillTemplateId);

        object? raw = await command.ExecuteScalarAsync(cancellationToken);

        return raw is null || raw is DBNull ? null : Convert.ToString(raw);
    }

    private async Task InsertLoadoutLogAsync(
        CharacterContext context,
        ulong actorDiscordUserId,
        string actionKey,
        uint slotNumber,
        ulong? previousSkillId,
        ulong? newSkillId,
        string? reason,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand logCommand = connection.CreateCommand();
        logCommand.CommandText = """
            INSERT INTO character_skill_loadout_logs (
                character_id,
                user_account_id,
                actor_discord_user_id,
                action_key,
                slot_number,
                previous_skill_template_id,
                new_skill_template_id,
                reason,
                created_at
            )
            VALUES (
                @character_id,
                @user_account_id,
                @actor_discord_user_id,
                @action_key,
                @slot_number,
                @previous_skill_template_id,
                @new_skill_template_id,
                @reason,
                CURRENT_TIMESTAMP
            );
            """;

        AddParameter(logCommand, "@character_id", context.CharacterId);
        AddParameter(logCommand, "@user_account_id", context.UserAccountId);
        AddParameter(logCommand, "@actor_discord_user_id", actorDiscordUserId.ToString());
        AddParameter(logCommand, "@action_key", actionKey);
        AddParameter(logCommand, "@slot_number", slotNumber);
        AddParameter(logCommand, "@previous_skill_template_id", previousSkillId);
        AddParameter(logCommand, "@new_skill_template_id", newSkillId);
        AddParameter(logCommand, "@reason", reason);

        await logCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task InsertUnlockLogAsync(
        CharacterContext context,
        ulong actorDiscordUserId,
        ulong skillTemplateId,
        string actionKey,
        string sourceKey,
        string? reason,
        CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand logCommand = connection.CreateCommand();
        logCommand.CommandText = """
            INSERT INTO character_skill_unlock_logs (
                character_id,
                user_account_id,
                actor_discord_user_id,
                skill_template_id,
                action_key,
                source_key,
                reason,
                created_at
            )
            VALUES (
                @character_id,
                @user_account_id,
                @actor_discord_user_id,
                @skill_template_id,
                @action_key,
                @source_key,
                @reason,
                CURRENT_TIMESTAMP
            );
            """;

        AddParameter(logCommand, "@character_id", context.CharacterId);
        AddParameter(logCommand, "@user_account_id", context.UserAccountId);
        AddParameter(logCommand, "@actor_discord_user_id", actorDiscordUserId.ToString());
        AddParameter(logCommand, "@skill_template_id", skillTemplateId);
        AddParameter(logCommand, "@action_key", actionKey);
        AddParameter(logCommand, "@source_key", sourceKey);
        AddParameter(logCommand, "@reason", reason);

        await logCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureDefaultSlotsAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM skill_slot_unlocks;";
        object? rawCount = await countCommand.ExecuteScalarAsync(cancellationToken);
        long count = rawCount is null || rawCount is DBNull ? 0 : Convert.ToInt64(rawCount);

        if (count >= 20)
        {
            return;
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO skill_slot_unlocks (
                slot_number,
                slot_type,
                unlock_level,
                name,
                description,
                display_order
            )
            VALUES
            (1, 'active', 1, 'Habilidad activa I', 'Primera habilidad activa disponible desde el inicio.', 1),
            (2, 'active', 5, 'Habilidad activa II', 'Segundo espacio de habilidad activa.', 2),
            (3, 'passive', 10, 'Pasiva I', 'Primer espacio pasivo.', 3),
            (4, 'role', 15, 'Rol/Exploración I', 'Primer espacio para habilidad de rol o exploración.', 4),
            (5, 'active', 20, 'Habilidad activa III', 'Tercer espacio activo.', 5),
            (6, 'passive', 25, 'Pasiva II', 'Segundo espacio pasivo.', 6),
            (7, 'stat', 30, 'Estadística I', 'Primer espacio para mejora estadística.', 7),
            (8, 'active', 35, 'Habilidad activa IV', 'Cuarto espacio activo.', 8),
            (9, 'role', 40, 'Rol/Exploración II', 'Segundo espacio de rol o exploración.', 9),
            (10, 'passive', 45, 'Pasiva III', 'Tercer espacio pasivo.', 10),
            (11, 'active', 50, 'Habilidad activa V', 'Quinto espacio activo.', 11),
            (12, 'passive', 55, 'Pasiva IV', 'Cuarto espacio pasivo.', 12),
            (13, 'stat', 60, 'Estadística II', 'Segundo espacio para mejora estadística.', 13),
            (14, 'role', 65, 'Rol/Exploración III', 'Tercer espacio de rol o exploración.', 14),
            (15, 'active', 70, 'Habilidad activa VI', 'Sexto espacio activo.', 15),
            (16, 'passive', 75, 'Pasiva V', 'Quinto espacio pasivo.', 16),
            (17, 'role', 80, 'Rol/Exploración IV', 'Cuarto espacio de rol o exploración.', 17),
            (18, 'active', 85, 'Habilidad activa VII', 'Séptimo espacio activo.', 18),
            (19, 'passive', 90, 'Pasiva VI', 'Sexto espacio pasivo.', 19),
            (20, 'active', 95, 'Habilidad activa VIII', 'Octavo y último espacio activo.', 20)
            ON DUPLICATE KEY UPDATE
                slot_type = VALUES(slot_type),
                unlock_level = VALUES(unlock_level),
                name = VALUES(name),
                description = VALUES(description),
                display_order = VALUES(display_order);
            """;

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

    private static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        string normalized = category.Trim().ToLowerInvariant();

        return normalized switch
        {
            "activa" or "activo" or "active" => "active",
            "pasiva" or "pasivo" or "passive" => "passive",
            "rol" or "role" or "exploracion" or "exploración" => "role",
            "stat" or "stats" or "estadistica" or "estadística" => "stat",
            _ => normalized
        };
    }

    private sealed record CharacterContext(
        ulong CharacterId,
        string CharacterName,
        uint Level,
        ulong UserAccountId,
        string NationKey,
        string ProfessionKey,
        string RoleKey);
}
