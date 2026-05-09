using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed partial class CharacterService : ICharacterService
{
    private readonly GameDbContext _dbContext;

    public CharacterService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CharacterCommandResult<CharacterProfileDto>> CreateAsync(
        ulong actorDiscordUserId,
        CharacterCreateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string name = NormalizeName(request.Name);

        if (string.IsNullOrWhiteSpace(name))
        {
            return Fail<CharacterProfileDto>("El nombre del personaje no puede estar vacío.");
        }

        if (name.Length > 120)
        {
            return Fail<CharacterProfileDto>("El nombre del personaje no puede superar 120 caracteres.");
        }

        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        UserAccountInfo? user = await GetUserAccountAsync(connection, actorDiscordUserId, cancellationToken);

        if (user is null)
        {
            return Fail<CharacterProfileDto>("Primero debes registrarte con `/registro` o `!registro`.");
        }

        if (!user.AccountStatus.Equals("active", StringComparison.OrdinalIgnoreCase))
        {
            return Fail<CharacterProfileDto>("Tu cuenta no está activa.");
        }

        if (await CharacterNameExistsAsync(connection, user.UserAccountId, name, null, cancellationToken))
        {
            return Fail<CharacterProfileDto>(
                $"Ya tienes un personaje activo llamado **{name}**. Usa otro nombre para no confundir la ficha.");
        }

        string? normalizedNicknameForCreate = NormalizeNullable(request.Nickname);

        if (normalizedNicknameForCreate is not null &&
            await CharacterNicknameExistsAsync(connection, user.UserAccountId, normalizedNicknameForCreate, null, cancellationToken))
        {
            return Fail<CharacterProfileDto>(
                $"Ya tienes un personaje activo con el apodo **{normalizedNicknameForCreate}**. Usa otro apodo o déjalo vacío.");
        }

        uint activeCount = await CountCharactersAsync(connection, user.UserAccountId, "active", cancellationToken);

        if (activeCount >= user.MaxRosterSlots)
        {
            return Fail<CharacterProfileDto>(
                $"Llegaste al límite de personajes registrados: {activeCount}/{user.MaxRosterSlots}. " +
                "Archiva uno o amplía el roster antes de crear otro.");
        }

        CatalogRef? nation = await ResolveNationAsync(connection, request.NationKeyOrName, cancellationToken);
        CatalogRef? role = await ResolveRoleAsync(connection, request.RoleKeyOrName, cancellationToken);
        CatalogRef? profession = await ResolveProfessionAsync(connection, request.ProfessionKeyOrName, cancellationToken);

        if (nation is null)
        {
            return Fail<CharacterProfileDto>($"No encontré la nación `{request.NationKeyOrName}`.");
        }

        if (role is null)
        {
            return Fail<CharacterProfileDto>($"No encontré el rol `{request.RoleKeyOrName}`.");
        }

        if (profession is null)
        {
            return Fail<CharacterProfileDto>($"No encontré la profesión `{request.ProfessionKeyOrName}`.");
        }

        await using DbCommand insertCommand = connection.CreateCommand();
        insertCommand.CommandText = """
            INSERT INTO characters (
                user_account_id,
                name,
                nickname,
                image_url,
                level,
                current_xp,
                total_xp,
                nation_id,
                role_id,
                profession_id,
                character_status,
                created_at,
                updated_at
            )
            VALUES (
                @user_account_id,
                @name,
                @nickname,
                @image_url,
                1,
                0,
                0,
                @nation_id,
                @role_id,
                @profession_id,
                'active',
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            );
            """;

        AddParameter(insertCommand, "@user_account_id", user.UserAccountId);
        AddParameter(insertCommand, "@name", name);
        AddParameter(insertCommand, "@nickname", NormalizeNullable(request.Nickname));
        AddParameter(insertCommand, "@image_url", NormalizeNullable(request.ImageUrl));
        AddParameter(insertCommand, "@nation_id", nation.Id);
        AddParameter(insertCommand, "@role_id", role.Id);
        AddParameter(insertCommand, "@profession_id", profession.Id);

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);

        await using DbCommand lastIdCommand = connection.CreateCommand();
        lastIdCommand.CommandText = "SELECT LAST_INSERT_ID();";
        ulong characterId = Convert.ToUInt64(await lastIdCommand.ExecuteScalarAsync(cancellationToken));

        await InitializeStatsAsync(connection, characterId, cancellationToken);
        await RecalculateBaseStatsAsync(connection, characterId, cancellationToken);

        bool hasActive = await HasActiveCharacterAsync(connection, user.UserAccountId, cancellationToken);

        if (!hasActive)
        {
            await SetActiveCharacterAsync(connection, user.UserAccountId, characterId, cancellationToken);
        }

        CharacterProfileDto? profile = await LoadProfileByIdAsync(connection, characterId, includeArchived: false, cancellationToken);

        return profile is null
            ? Fail<CharacterProfileDto>("El personaje fue creado, pero no se pudo cargar su ficha.")
            : Ok("Personaje creado correctamente.", profile);
    }

    public async Task<CharacterCommandResult<IReadOnlyList<CharacterListItemDto>>> ListAsync(
        ulong actorDiscordUserId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        UserAccountInfo? user = await GetUserAccountAsync(connection, actorDiscordUserId, cancellationToken);

        if (user is null)
        {
            return Fail<IReadOnlyList<CharacterListItemDto>>("Primero debes registrarte con `/registro` o `!registro`.");
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                characters.id,
                characters.name,
                characters.nickname,
                characters.image_url,
                characters.level,
                characters.current_xp,
                characters.total_xp,
                characters.character_status,
                characters.created_at,
                nations.name AS nation_name,
                roles.name AS role_name,
                professions.name AS profession_name,
                CASE WHEN user_active_characters.character_id IS NULL THEN 0 ELSE 1 END AS is_active_character
            FROM characters
            INNER JOIN nations
                ON nations.id = characters.nation_id
            INNER JOIN roles
                ON roles.id = characters.role_id
            INNER JOIN professions
                ON professions.id = characters.profession_id
            LEFT JOIN user_active_characters
                ON user_active_characters.user_account_id = characters.user_account_id
               AND user_active_characters.character_id = characters.id
            WHERE characters.user_account_id = @user_account_id
              {(includeArchived ? "" : "AND characters.character_status = 'active'")}
            ORDER BY
                CASE characters.character_status
                    WHEN 'active' THEN 1
                    WHEN 'archived' THEN 2
                    ELSE 3
                END,
                characters.created_at DESC;
            """;

        AddParameter(command, "@user_account_id", user.UserAccountId);

        List<CharacterListItemDto> items = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new CharacterListItemDto(
                Convert.ToUInt64(reader["id"]),
                Convert.ToString(reader["name"]) ?? string.Empty,
                reader["nickname"] is DBNull ? null : Convert.ToString(reader["nickname"]),
                reader["image_url"] is DBNull ? null : Convert.ToString(reader["image_url"]),
                Convert.ToUInt32(reader["level"]),
                Convert.ToUInt64(reader["current_xp"]),
                Convert.ToUInt64(reader["total_xp"]),
                Convert.ToString(reader["nation_name"]) ?? string.Empty,
                Convert.ToString(reader["role_name"]) ?? string.Empty,
                Convert.ToString(reader["profession_name"]) ?? string.Empty,
                Convert.ToString(reader["character_status"]) ?? "active",
                Convert.ToInt32(reader["is_active_character"]) == 1,
                Convert.ToDateTime(reader["created_at"])));
        }

        return Ok(items.Count == 0 ? "No tienes personajes registrados." : "Personajes encontrados.", (IReadOnlyList<CharacterListItemDto>)items);
    }

    public async Task<CharacterCommandResult<CharacterProfileDto>> GetAsync(
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        CharacterProfileDto? profile = await ResolveProfileAsync(
            connection,
            actorDiscordUserId,
            lookup,
            includeArchived,
            allowAnyOwner: true,
            cancellationToken);

        return profile is null
            ? Fail<CharacterProfileDto>("No encontré el personaje solicitado.")
            : Ok("Ficha encontrada.", profile);
    }

    public async Task<CharacterCommandResult<CharacterProfileDto>> SelectAsync(
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        UserAccountInfo? user = await GetUserAccountAsync(connection, actorDiscordUserId, cancellationToken);

        if (user is null)
        {
            return Fail<CharacterProfileDto>("Primero debes registrarte con `/registro` o `!registro`.");
        }

        CharacterProfileDto? profile = await ResolveProfileAsync(
            connection,
            actorDiscordUserId,
            lookup,
            includeArchived: false,
            allowAnyOwner: false,
            cancellationToken);

        if (profile is null)
        {
            return Fail<CharacterProfileDto>("No encontré un personaje activo tuyo con esos datos.");
        }

        await SetActiveCharacterAsync(connection, user.UserAccountId, profile.CharacterId, cancellationToken);

        CharacterProfileDto? updated = await LoadProfileByIdAsync(connection, profile.CharacterId, includeArchived: false, cancellationToken);

        return updated is null
            ? Fail<CharacterProfileDto>("Se seleccionó el personaje, pero no pude recargar la ficha.")
            : Ok($"**{updated.Name}** ahora es tu personaje activo.", updated);
    }

    public async Task<CharacterCommandResult<CharacterProfileDto>> EditAsync(
        ulong actorDiscordUserId,
        CharacterEditRequestDto request,
        CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        CharacterLookupDto lookup = new(
            request.CharacterId,
            request.CurrentName,
            actorDiscordUserId);

        CharacterProfileDto? profile = await ResolveProfileAsync(
            connection,
            actorDiscordUserId,
            lookup,
            includeArchived: false,
            allowAnyOwner: false,
            cancellationToken);

        if (profile is null)
        {
            return Fail<CharacterProfileDto>("No encontré un personaje activo tuyo para editar.");
        }

        string? newName = NormalizeNullable(request.NewName);
        string? newNickname = NormalizeNullable(request.NewNickname);
        string? newImageUrl = NormalizeNullable(request.NewImageUrl);

        if (newName is null && newNickname is null && newImageUrl is null)
        {
            return Fail<CharacterProfileDto>("No enviaste ningún dato para actualizar.");
        }

        if (newName is not null &&
            await CharacterNameExistsAsync(connection, profile.UserAccountId, newName, profile.CharacterId, cancellationToken))
        {
            return Fail<CharacterProfileDto>(
                $"Ya tienes otro personaje activo llamado **{newName}**. Usa un nombre distinto.");
        }

        if (newNickname is not null &&
            await CharacterNicknameExistsAsync(connection, profile.UserAccountId, newNickname, profile.CharacterId, cancellationToken))
        {
            return Fail<CharacterProfileDto>(
                $"Ya tienes otro personaje activo con el apodo **{newNickname}**. Usa un apodo distinto o déjalo vacío.");
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE characters
            SET name = COALESCE(@new_name, name),
                nickname = COALESCE(@new_nickname, nickname),
                image_url = COALESCE(@new_image_url, image_url),
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @character_id
              AND user_account_id = @user_account_id
              AND character_status = 'active';
            """;

        AddParameter(command, "@new_name", newName);
        AddParameter(command, "@new_nickname", newNickname);
        AddParameter(command, "@new_image_url", newImageUrl);
        AddParameter(command, "@character_id", profile.CharacterId);
        AddParameter(command, "@user_account_id", profile.UserAccountId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        CharacterProfileDto? updated = await LoadProfileByIdAsync(connection, profile.CharacterId, includeArchived: false, cancellationToken);

        return updated is null
            ? Fail<CharacterProfileDto>("El personaje fue editado, pero no pude recargar la ficha.")
            : Ok($"**{updated.Name}** fue actualizado.", updated);
    }

    public async Task<CharacterCommandResult<CharacterProfileDto>> ArchiveAsync(
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        CharacterProfileDto? profile = await ResolveProfileAsync(
            connection,
            actorDiscordUserId,
            lookup,
            includeArchived: false,
            allowAnyOwner: false,
            cancellationToken);

        if (profile is null)
        {
            return Fail<CharacterProfileDto>("No encontré un personaje activo tuyo para archivar.");
        }

        await using DbCommand updateCommand = connection.CreateCommand();
        updateCommand.CommandText = """
            UPDATE characters
            SET character_status = 'archived',
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @character_id
              AND user_account_id = @user_account_id;
            """;

        AddParameter(updateCommand, "@character_id", profile.CharacterId);
        AddParameter(updateCommand, "@user_account_id", profile.UserAccountId);
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        await using DbCommand clearCommand = connection.CreateCommand();
        clearCommand.CommandText = """
            DELETE FROM user_active_characters
            WHERE user_account_id = @user_account_id
              AND character_id = @character_id;
            """;

        AddParameter(clearCommand, "@user_account_id", profile.UserAccountId);
        AddParameter(clearCommand, "@character_id", profile.CharacterId);
        await clearCommand.ExecuteNonQueryAsync(cancellationToken);

        CharacterProfileDto? archived = await LoadProfileByIdAsync(connection, profile.CharacterId, includeArchived: true, cancellationToken);

        return archived is null
            ? Fail<CharacterProfileDto>("El personaje fue archivado, pero no pude recargar la ficha.")
            : Ok($"**{archived.Name}** fue archivado. No se borraron sus datos.", archived);
    }

    public async Task<CharacterCommandResult<CharacterProfileDto>> RestoreAsync(
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        bool allowAnyOwner = false,
        CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        CharacterProfileDto? profile = await ResolveProfileAsync(
            connection,
            actorDiscordUserId,
            lookup,
            includeArchived: true,
            allowAnyOwner,
            cancellationToken);

        if (profile is null)
        {
            return Fail<CharacterProfileDto>("No encontré el personaje para restaurar.");
        }

        if (!allowAnyOwner && profile.OwnerDiscordUserId != actorDiscordUserId)
        {
            return Fail<CharacterProfileDto>("Solo puedes restaurar personajes propios.");
        }

        await using DbCommand updateCommand = connection.CreateCommand();
        updateCommand.CommandText = """
            UPDATE characters
            SET character_status = 'active',
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @character_id;
            """;

        AddParameter(updateCommand, "@character_id", profile.CharacterId);
        await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        CharacterProfileDto? restored = await LoadProfileByIdAsync(connection, profile.CharacterId, includeArchived: false, cancellationToken);

        return restored is null
            ? Fail<CharacterProfileDto>("El personaje fue restaurado, pero no pude recargar la ficha.")
            : Ok($"**{restored.Name}** fue restaurado.", restored);
    }

    private async Task<CharacterProfileDto?> ResolveProfileAsync(
        DbConnection connection,
        ulong actorDiscordUserId,
        CharacterLookupDto lookup,
        bool includeArchived,
        bool allowAnyOwner,
        CancellationToken cancellationToken)
    {
        if (lookup.CharacterId is not null && lookup.CharacterId.Value > 0)
        {
            CharacterProfileDto? byId = await LoadProfileByIdAsync(connection, lookup.CharacterId.Value, includeArchived, cancellationToken);

            if (byId is null)
            {
                return null;
            }

            if (!allowAnyOwner && byId.OwnerDiscordUserId != actorDiscordUserId)
            {
                return null;
            }

            return byId;
        }

        ulong targetDiscordId = lookup.TargetDiscordUserId ?? actorDiscordUserId;

        if (!string.IsNullOrWhiteSpace(lookup.CharacterName))
        {
            return await LoadProfileByOwnerAndNameAsync(
                connection,
                targetDiscordId,
                lookup.CharacterName!,
                includeArchived,
                cancellationToken);
        }

        CharacterProfileDto? active = await LoadActiveProfileAsync(
            connection,
            targetDiscordId,
            includeArchived,
            cancellationToken);

        if (active is not null)
        {
            return active;
        }

        return await LoadFirstProfileAsync(connection, targetDiscordId, includeArchived, cancellationToken);
    }

    private async Task<CharacterProfileDto?> LoadProfileByIdAsync(
        DbConnection connection,
        ulong characterId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        return await LoadSingleProfileAsync(
            connection,
            "characters.id = @character_id",
            includeArchived,
            cancellationToken,
            ("@character_id", characterId));
    }

    private async Task<CharacterProfileDto?> LoadProfileByOwnerAndNameAsync(
        DbConnection connection,
        ulong discordUserId,
        string characterName,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        return await LoadSingleProfileAsync(
            connection,
            "user_accounts.discord_user_id = @discord_user_id AND LOWER(characters.name) = LOWER(@character_name)",
            includeArchived,
            cancellationToken,
            ("@discord_user_id", discordUserId.ToString()),
            ("@character_name", NormalizeName(characterName)));
    }

    private async Task<CharacterProfileDto?> LoadActiveProfileAsync(
        DbConnection connection,
        ulong discordUserId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        return await LoadSingleProfileAsync(
            connection,
            "user_accounts.discord_user_id = @discord_user_id AND user_active_characters.character_id = characters.id",
            includeArchived,
            cancellationToken,
            ("@discord_user_id", discordUserId.ToString()));
    }

    private async Task<CharacterProfileDto?> LoadFirstProfileAsync(
        DbConnection connection,
        ulong discordUserId,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        return await LoadSingleProfileAsync(
            connection,
            "user_accounts.discord_user_id = @discord_user_id",
            includeArchived,
            cancellationToken,
            ("@discord_user_id", discordUserId.ToString()));
    }

    private async Task<CharacterProfileDto?> LoadSingleProfileAsync(
        DbConnection connection,
        string whereClause,
        bool includeArchived,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        ulong characterId;
        ulong userAccountId;
        ulong ownerDiscordUserId;
        string ownerUsername;
        string name;
        string? nickname;
        string? imageUrl;
        uint level;
        ulong currentXp;
        ulong totalXp;
        string characterStatus;
        DateTime createdAt;
        string nationKey;
        string nationName;
        string? nationIconUrl;
        string? nationBannerUrl;
        string roleKey;
        string roleName;
        string? roleIconUrl;
        string? roleBannerUrl;
        string professionKey;
        string professionName;
        string? professionIconUrl;
        string? professionBannerUrl;
        bool isActiveCharacter;

        await using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT
                    characters.id AS character_id,
                    characters.user_account_id,
                    user_accounts.discord_user_id AS owner_discord_user_id,
                    user_accounts.username AS owner_username,
                    characters.name,
                    characters.nickname,
                    characters.image_url,
                    characters.level,
                    characters.current_xp,
                    characters.total_xp,
                    characters.character_status,
                    characters.created_at,
                    nations.nation_key,
                    nations.name AS nation_name,
                    nations.icon_url AS nation_icon_url,
                    nations.banner_url AS nation_banner_url,
                    roles.role_key,
                    roles.name AS role_name,
                    roles.icon_url AS role_icon_url,
                    roles.banner_url AS role_banner_url,
                    professions.profession_key,
                    professions.name AS profession_name,
                    professions.icon_url AS profession_icon_url,
                    professions.banner_url AS profession_banner_url,
                    CASE WHEN user_active_characters.character_id IS NULL THEN 0 ELSE 1 END AS is_active_character
                FROM characters
                INNER JOIN user_accounts
                    ON user_accounts.id = characters.user_account_id
                INNER JOIN nations
                    ON nations.id = characters.nation_id
                INNER JOIN roles
                    ON roles.id = characters.role_id
                INNER JOIN professions
                    ON professions.id = characters.profession_id
                LEFT JOIN user_active_characters
                    ON user_active_characters.user_account_id = characters.user_account_id
                   AND user_active_characters.character_id = characters.id
                WHERE {whereClause}
                  {(includeArchived ? "" : "AND characters.character_status = 'active'")}
                ORDER BY characters.created_at DESC
                LIMIT 1;
                """;

            foreach ((string parameterName, object? value) in parameters)
            {
                AddParameter(command, parameterName, value);
            }

            await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            characterId = Convert.ToUInt64(reader["character_id"]);
            userAccountId = Convert.ToUInt64(reader["user_account_id"]);
            ownerDiscordUserId = Convert.ToUInt64(reader["owner_discord_user_id"]);
            ownerUsername = Convert.ToString(reader["owner_username"]) ?? string.Empty;
            name = Convert.ToString(reader["name"]) ?? string.Empty;
            nickname = reader["nickname"] is DBNull ? null : Convert.ToString(reader["nickname"]);
            imageUrl = reader["image_url"] is DBNull ? null : Convert.ToString(reader["image_url"]);
            level = Convert.ToUInt32(reader["level"]);
            currentXp = Convert.ToUInt64(reader["current_xp"]);
            totalXp = Convert.ToUInt64(reader["total_xp"]);
            characterStatus = Convert.ToString(reader["character_status"]) ?? "active";
            createdAt = Convert.ToDateTime(reader["created_at"]);
            nationKey = Convert.ToString(reader["nation_key"]) ?? string.Empty;
            nationName = Convert.ToString(reader["nation_name"]) ?? string.Empty;
            nationIconUrl = reader["nation_icon_url"] is DBNull ? null : Convert.ToString(reader["nation_icon_url"]);
            nationBannerUrl = reader["nation_banner_url"] is DBNull ? null : Convert.ToString(reader["nation_banner_url"]);
            roleKey = Convert.ToString(reader["role_key"]) ?? string.Empty;
            roleName = Convert.ToString(reader["role_name"]) ?? string.Empty;
            roleIconUrl = reader["role_icon_url"] is DBNull ? null : Convert.ToString(reader["role_icon_url"]);
            roleBannerUrl = reader["role_banner_url"] is DBNull ? null : Convert.ToString(reader["role_banner_url"]);
            professionKey = Convert.ToString(reader["profession_key"]) ?? string.Empty;
            professionName = Convert.ToString(reader["profession_name"]) ?? string.Empty;
            professionIconUrl = reader["profession_icon_url"] is DBNull ? null : Convert.ToString(reader["profession_icon_url"]);
            professionBannerUrl = reader["profession_banner_url"] is DBNull ? null : Convert.ToString(reader["profession_banner_url"]);
            isActiveCharacter = Convert.ToInt32(reader["is_active_character"]) == 1;
        }

        CharacterEquipmentSummaryDto equipment = new(
            "Sin arma equipada",
            "Artefactos: 0/5 · ArtUnic: sin equipar",
            "Sin ArtUnic equipado");

        CharacterSkillTreeSummaryDto skillTree = await LoadSkillTreeAsync(
            connection,
            characterId,
            level,
            cancellationToken);

        IReadOnlyList<CharacterStatValueDto> stats = await LoadStatsAsync(
            connection,
            characterId,
            cancellationToken);

        return new CharacterProfileDto(
            characterId,
            userAccountId,
            ownerDiscordUserId,
            ownerUsername,
            name,
            nickname,
            imageUrl,
            level,
            currentXp,
            totalXp,
            nationKey,
            nationName,
            nationIconUrl,
            nationBannerUrl,
            roleKey,
            roleName,
            roleIconUrl,
            roleBannerUrl,
            professionKey,
            professionName,
            professionIconUrl,
            professionBannerUrl,
            isActiveCharacter,
            characterStatus,
            createdAt,
            equipment,
            skillTree,
            stats);
    }

    private async Task<IReadOnlyList<CharacterStatValueDto>> LoadStatsAsync(
        DbConnection connection,
        ulong characterId,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
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
            ORDER BY stat_types.display_order, stat_types.id;
            """;

        AddParameter(command, "@character_id", characterId);

        List<CharacterStatValueDto> stats = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            stats.Add(new CharacterStatValueDto(
                Convert.ToString(reader["stat_key"]) ?? string.Empty,
                Convert.ToString(reader["name"]) ?? string.Empty,
                Convert.ToString(reader["value_kind"]) ?? string.Empty,
                Convert.ToDecimal(reader["base_value"]),
                Convert.ToDecimal(reader["extra_value"]),
                Convert.ToDecimal(reader["total_value"])));
        }

        return stats;
    }

    private async Task<CharacterSkillTreeSummaryDto> LoadSkillTreeAsync(
        DbConnection connection,
        ulong characterId,
        uint level,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                skill_slot_unlocks.slot_number,
                skill_slot_unlocks.slot_category,
                skill_slot_unlocks.unlock_level,
                skill_slot_unlocks.name AS slot_name,
                skill_templates.id AS skill_template_id,
                skill_templates.name AS skill_name,
                skill_templates.skill_category AS skill_category
            FROM skill_slot_unlocks
            LEFT JOIN character_skill_loadout_slots
                ON character_skill_loadout_slots.slot_number = skill_slot_unlocks.slot_number
               AND character_skill_loadout_slots.character_id = @character_id
            LEFT JOIN skill_templates
                ON skill_templates.id = character_skill_loadout_slots.skill_template_id
            ORDER BY skill_slot_unlocks.slot_number;
            """;

        AddParameter(command, "@character_id", characterId);

        List<CharacterSkillSlotDto> slots = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            uint unlockLevel = Convert.ToUInt32(reader["unlock_level"]);
            ulong? skillId = reader["skill_template_id"] is DBNull ? null : Convert.ToUInt64(reader["skill_template_id"]);

            slots.Add(new CharacterSkillSlotDto(
                Convert.ToUInt32(reader["slot_number"]),
                Convert.ToString(reader["slot_category"]) ?? string.Empty,
                unlockLevel,
                Convert.ToString(reader["slot_name"]) ?? string.Empty,
                level >= unlockLevel,
                skillId,
                reader["skill_name"] is DBNull ? null : Convert.ToString(reader["skill_name"]),
                reader["skill_category"] is DBNull ? null : Convert.ToString(reader["skill_category"])));
        }

        return new CharacterSkillTreeSummaryDto(
            (uint)slots.Count,
            (uint)slots.Count(slot => slot.IsUnlocked),
            (uint)slots.Count(slot => slot.SkillTemplateId is not null),
            slots);
    }

    private async Task InitializeStatsAsync(
        DbConnection connection,
        ulong characterId,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO character_stats (
                character_id,
                stat_type_id,
                base_value,
                extra_value
            )
            SELECT
                @character_id,
                stat_types.id,
                stat_types.default_base,
                0
            FROM stat_types
            WHERE stat_types.is_active = TRUE
            ON DUPLICATE KEY UPDATE
                base_value = VALUES(base_value),
                extra_value = character_stats.extra_value;
            """;

        AddParameter(command, "@character_id", characterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task RecalculateBaseStatsAsync(
        DbConnection connection,
        ulong characterId,
        CancellationToken cancellationToken)
    {
        CharacterBaseContext? context = await LoadCharacterBaseContextAsync(connection, characterId, cancellationToken);

        if (context is null)
        {
            return;
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO character_stats (
                character_id,
                stat_type_id,
                base_value,
                extra_value
            )
            SELECT
                @character_id,
                stat_types.id,
                ROUND(
                    stat_types.default_base
                    + ((@level - 1) * COALESCE(SUM(stat_growth_rules.growth_per_level), 0))
                    + COALESCE(SUM(stat_growth_rules.flat_bonus), 0),
                    2
                ) AS base_value,
                COALESCE(existing_stats.extra_value, 0) AS extra_value
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
            LEFT JOIN character_stats AS existing_stats
                ON existing_stats.character_id = @character_id
               AND existing_stats.stat_type_id = stat_types.id
            WHERE stat_types.is_active = TRUE
            GROUP BY
                stat_types.id,
                stat_types.default_base,
                existing_stats.extra_value
            ON DUPLICATE KEY UPDATE
                base_value = VALUES(base_value),
                extra_value = VALUES(extra_value),
                updated_at = CURRENT_TIMESTAMP;
            """;

        AddParameter(command, "@character_id", characterId);
        AddParameter(command, "@level", context.Level);
        AddParameter(command, "@nation_key", context.NationKey);
        AddParameter(command, "@role_key", context.RoleKey);
        AddParameter(command, "@profession_key", context.ProfessionKey);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<CharacterBaseContext?> LoadCharacterBaseContextAsync(
        DbConnection connection,
        ulong characterId,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
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
            LIMIT 1;
            """;

        AddParameter(command, "@character_id", characterId);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterBaseContext(
            Convert.ToUInt32(reader["level"]),
            Convert.ToString(reader["nation_key"]) ?? string.Empty,
            Convert.ToString(reader["role_key"]) ?? string.Empty,
            Convert.ToString(reader["profession_key"]) ?? string.Empty);
    }

    private async Task<bool> HasActiveCharacterAsync(
        DbConnection connection,
        ulong userAccountId,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM user_active_characters
            WHERE user_account_id = @user_account_id;
            """;

        AddParameter(command, "@user_account_id", userAccountId);

        return Convert.ToUInt64(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private async Task SetActiveCharacterAsync(
        DbConnection connection,
        ulong userAccountId,
        ulong characterId,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO user_active_characters (
                user_account_id,
                character_id,
                selected_at
            )
            VALUES (
                @user_account_id,
                @character_id,
                CURRENT_TIMESTAMP
            )
            ON DUPLICATE KEY UPDATE
                character_id = VALUES(character_id),
                selected_at = CURRENT_TIMESTAMP;
            """;

        AddParameter(command, "@user_account_id", userAccountId);
        AddParameter(command, "@character_id", characterId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<UserAccountInfo?> GetUserAccountAsync(
        DbConnection connection,
        ulong discordUserId,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                id,
                username,
                account_status,
                max_roster_slots
            FROM user_accounts
            WHERE discord_user_id = @discord_user_id
            LIMIT 1;
            """;

        AddParameter(command, "@discord_user_id", discordUserId.ToString());

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new UserAccountInfo(
            Convert.ToUInt64(reader["id"]),
            Convert.ToString(reader["username"]) ?? string.Empty,
            Convert.ToString(reader["account_status"]) ?? "active",
            Convert.ToUInt32(reader["max_roster_slots"]));
    }

    private async Task<uint> CountCharactersAsync(
        DbConnection connection,
        ulong userAccountId,
        string status,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM characters
            WHERE user_account_id = @user_account_id
              AND character_status = @character_status;
            """;

        AddParameter(command, "@user_account_id", userAccountId);
        AddParameter(command, "@character_status", status);

        return Convert.ToUInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<CatalogRef?> ResolveNationAsync(DbConnection connection, string value, CancellationToken cancellationToken)
    {
        return await ResolveCatalogAsync(connection, "nations", "nation_key", value, cancellationToken);
    }

    private async Task<CatalogRef?> ResolveRoleAsync(DbConnection connection, string value, CancellationToken cancellationToken)
    {
        return await ResolveCatalogAsync(connection, "roles", "role_key", value, cancellationToken);
    }

    private async Task<CatalogRef?> ResolveProfessionAsync(DbConnection connection, string value, CancellationToken cancellationToken)
    {
        return await ResolveCatalogAsync(connection, "professions", "profession_key", value, cancellationToken);
    }

    private static async Task<CatalogRef?> ResolveCatalogAsync(
        DbConnection connection,
        string tableName,
        string keyColumn,
        string value,
        CancellationToken cancellationToken)
    {
        string normalized = NormalizeName(value);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                id,
                {keyColumn} AS catalog_key,
                name
            FROM {tableName}
            WHERE is_active = TRUE
              AND (
                    LOWER({keyColumn}) = LOWER(@value)
                 OR LOWER(name) = LOWER(@value)
              )
            LIMIT 1;
            """;

        AddParameter(command, "@value", normalized);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CatalogRef(
            Convert.ToUInt64(reader["id"]),
            Convert.ToString(reader["catalog_key"]) ?? string.Empty,
            Convert.ToString(reader["name"]) ?? string.Empty);
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

    private static string NormalizeName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static CharacterCommandResult<T> Ok<T>(string message, T data)
    {
        return new CharacterCommandResult<T>(true, message, data);
    }

    private static CharacterCommandResult<T> Fail<T>(string message)
    {
        return new CharacterCommandResult<T>(false, message, default);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private sealed record UserAccountInfo(
        ulong UserAccountId,
        string Username,
        string AccountStatus,
        uint MaxRosterSlots);

    private sealed record CatalogRef(
        ulong Id,
        string Key,
        string Name);

    private sealed record CharacterBaseContext(
        uint Level,
        string NationKey,
        string RoleKey,
        string ProfessionKey);
}
