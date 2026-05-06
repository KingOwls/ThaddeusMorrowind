using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;

namespace ThaddeusMorrowind.Bot.Features.Users;

public sealed class UserService : IUserService, IUserWalletService, IUserActivityService, IActiveCharacterService
{
    private readonly GameDbContext _dbContext;

    public UserService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfileDto> RegisterOrUpdateAsync(DiscordUserContextDto discordUser, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO user_accounts (
                    discord_user_id, username, public_nickname, avatar_url,
                    account_status, max_roster_slots, last_interaction_at,
                    registered_at, updated_at
                )
                VALUES (
                    @discord_user_id, @username, @public_nickname, @avatar_url,
                    'active', 4, CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
                )
                ON DUPLICATE KEY UPDATE
                    username = VALUES(username),
                    public_nickname = VALUES(public_nickname),
                    avatar_url = VALUES(avatar_url),
                    last_interaction_at = CURRENT_TIMESTAMP,
                    updated_at = CURRENT_TIMESTAMP;
                """;

            AddParameter(command, "@discord_user_id", discordUser.DiscordUserId.ToString());
            AddParameter(command, "@username", discordUser.Username);
            AddParameter(command, "@public_nickname", discordUser.PublicNickname);
            AddParameter(command, "@avatar_url", discordUser.AvatarUrl);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        ulong userAccountId = await GetUserAccountIdRequiredAsync(connection, discordUser.DiscordUserId, cancellationToken);
        await EnsureWalletAsync(userAccountId, cancellationToken);
        await LogAsync(discordUser.DiscordUserId, "user_registered_or_updated", "Registro o actualización del usuario.", null, cancellationToken);

        UserProfileDto? profile = await GetProfileAsync(discordUser.DiscordUserId, cancellationToken);
        return profile ?? throw new InvalidOperationException("No se pudo consultar el perfil después del registro.");
    }

    public async Task<UserProfileDto?> GetProfileAsync(ulong discordUserId, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                user_accounts.id AS user_account_id,
                user_accounts.discord_user_id,
                user_accounts.username,
                user_accounts.public_nickname,
                user_accounts.avatar_url,
                user_accounts.account_status,
                user_accounts.max_roster_slots,
                user_accounts.registered_at,
                user_accounts.last_interaction_at,
                COALESCE(user_wallets.gold, 0) AS gold,
                COALESCE(user_wallets.premium_currency, 0) AS premium_currency,
                (
                    SELECT COUNT(*)
                    FROM characters
                    WHERE characters.user_account_id = user_accounts.id
                      AND characters.character_status = 'active'
                ) AS character_count,
                active_character.id AS active_character_id,
                active_character.name AS active_character_name
            FROM user_accounts
            LEFT JOIN user_wallets
                ON user_wallets.user_account_id = user_accounts.id
            LEFT JOIN user_active_characters
                ON user_active_characters.user_account_id = user_accounts.id
            LEFT JOIN characters AS active_character
                ON active_character.id = user_active_characters.character_id
               AND active_character.user_account_id = user_accounts.id
               AND active_character.character_status = 'active'
            WHERE user_accounts.discord_user_id = @discord_user_id
            LIMIT 1;
            """;

        AddParameter(command, "@discord_user_id", discordUserId.ToString());

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProfile(reader) : null;
    }

    public async Task<UserProfileDto?> UpdateDiscordIdentityAsync(DiscordUserContextDto discordUser, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE user_accounts
            SET username = @username,
                public_nickname = @public_nickname,
                avatar_url = @avatar_url,
                last_interaction_at = CURRENT_TIMESTAMP,
                updated_at = CURRENT_TIMESTAMP
            WHERE discord_user_id = @discord_user_id;
            """;

        AddParameter(command, "@username", discordUser.Username);
        AddParameter(command, "@public_nickname", discordUser.PublicNickname);
        AddParameter(command, "@avatar_url", discordUser.AvatarUrl);
        AddParameter(command, "@discord_user_id", discordUser.DiscordUserId.ToString());

        int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affectedRows <= 0)
        {
            return null;
        }

        await LogAsync(discordUser.DiscordUserId, "user_profile_refreshed", "Perfil sincronizado con Discord.", null, cancellationToken);
        return await GetProfileAsync(discordUser.DiscordUserId, cancellationToken);
    }

    public async Task TouchLastInteractionAsync(ulong discordUserId, string activityKey, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE user_accounts
            SET last_interaction_at = CURRENT_TIMESTAMP
            WHERE discord_user_id = @discord_user_id;
            """;

        AddParameter(command, "@discord_user_id", discordUserId.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
        await LogAsync(discordUserId, activityKey, "Interacción del usuario con el bot.", null, cancellationToken);
    }

    public async Task<UserWalletDto> EnsureWalletAsync(ulong userAccountId, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using (DbCommand command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO user_wallets (user_account_id, gold, premium_currency)
                VALUES (@user_account_id, 0, 0)
                ON DUPLICATE KEY UPDATE user_account_id = VALUES(user_account_id);
                """;
            AddParameter(command, "@user_account_id", userAccountId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using DbCommand selectCommand = connection.CreateCommand();
        selectCommand.CommandText = """
            SELECT user_account_id, gold, premium_currency, updated_at
            FROM user_wallets
            WHERE user_account_id = @user_account_id
            LIMIT 1;
            """;
        AddParameter(selectCommand, "@user_account_id", userAccountId);

        await using DbDataReader reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("No se pudo crear la wallet.");
        }

        return ReadWallet(reader);
    }

    public async Task<UserWalletDto?> GetWalletAsync(ulong discordUserId, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT user_wallets.user_account_id, user_wallets.gold, user_wallets.premium_currency, user_wallets.updated_at
            FROM user_wallets
            INNER JOIN user_accounts
                ON user_accounts.id = user_wallets.user_account_id
            WHERE user_accounts.discord_user_id = @discord_user_id
            LIMIT 1;
            """;
        AddParameter(command, "@discord_user_id", discordUserId.ToString());

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadWallet(reader) : null;
    }

    public async Task LogAsync(ulong discordUserId, string activityKey, string? description = null, string? payloadJson = null, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        ulong? userAccountId = await GetUserAccountIdAsync(connection, discordUserId, cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO user_activity_logs (user_account_id, discord_user_id, activity_key, description, payload_json, created_at)
            VALUES (@user_account_id, @discord_user_id, @activity_key, @description, @payload_json, CURRENT_TIMESTAMP);
            """;
        AddParameter(command, "@user_account_id", userAccountId);
        AddParameter(command, "@discord_user_id", discordUserId.ToString());
        AddParameter(command, "@activity_key", activityKey);
        AddParameter(command, "@description", description);
        AddParameter(command, "@payload_json", payloadJson);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ActiveCharacterDto?> GetActiveCharacterAsync(ulong discordUserId, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                characters.id AS character_id,
                characters.name,
                characters.nickname,
                characters.image_url,
                characters.level,
                nations.name AS nation_name,
                roles.name AS role_name,
                professions.name AS profession_name
            FROM user_active_characters
            INNER JOIN user_accounts
                ON user_accounts.id = user_active_characters.user_account_id
            INNER JOIN characters
                ON characters.id = user_active_characters.character_id
               AND characters.user_account_id = user_accounts.id
            INNER JOIN nations ON nations.id = characters.nation_id
            INNER JOIN roles ON roles.id = characters.role_id
            INNER JOIN professions ON professions.id = characters.profession_id
            WHERE user_accounts.discord_user_id = @discord_user_id
              AND characters.character_status = 'active'
            LIMIT 1;
            """;
        AddParameter(command, "@discord_user_id", discordUserId.ToString());

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        return new ActiveCharacterDto(
            Convert.ToUInt64(reader["character_id"]),
            Convert.ToString(reader["name"]) ?? string.Empty,
            reader["nickname"] is DBNull ? null : Convert.ToString(reader["nickname"]),
            reader["image_url"] is DBNull ? null : Convert.ToString(reader["image_url"]),
            Convert.ToUInt32(reader["level"]),
            Convert.ToString(reader["nation_name"]) ?? string.Empty,
            Convert.ToString(reader["role_name"]) ?? string.Empty,
            Convert.ToString(reader["profession_name"]) ?? string.Empty);
    }

    public async Task<bool> SetActiveCharacterAsync(ulong discordUserId, ulong characterId, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        ulong? userAccountId = await GetCharacterOwnerAsync(connection, discordUserId, characterId, cancellationToken);
        if (userAccountId is null) return false;

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO user_active_characters (user_account_id, character_id, selected_at)
            VALUES (@user_account_id, @character_id, CURRENT_TIMESTAMP)
            ON DUPLICATE KEY UPDATE character_id = VALUES(character_id), selected_at = CURRENT_TIMESTAMP;
            """;
        AddParameter(command, "@user_account_id", userAccountId.Value);
        AddParameter(command, "@character_id", characterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return true;
    }

    public async Task ClearActiveCharacterAsync(ulong discordUserId, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            DELETE user_active_characters
            FROM user_active_characters
            INNER JOIN user_accounts ON user_accounts.id = user_active_characters.user_account_id
            WHERE user_accounts.discord_user_id = @discord_user_id;
            """;
        AddParameter(command, "@discord_user_id", discordUserId.ToString());
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

    private static async Task<ulong?> GetUserAccountIdAsync(DbConnection connection, ulong discordUserId, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM user_accounts WHERE discord_user_id = @discord_user_id LIMIT 1;";
        AddParameter(command, "@discord_user_id", discordUserId.ToString());
        object? raw = await command.ExecuteScalarAsync(cancellationToken);
        return raw is null || raw is DBNull ? null : Convert.ToUInt64(raw);
    }

    private static async Task<ulong> GetUserAccountIdRequiredAsync(DbConnection connection, ulong discordUserId, CancellationToken cancellationToken)
    {
        ulong? id = await GetUserAccountIdAsync(connection, discordUserId, cancellationToken);
        return id ?? throw new InvalidOperationException("No se encontró el usuario después del registro.");
    }

    private static async Task<ulong?> GetCharacterOwnerAsync(DbConnection connection, ulong discordUserId, ulong characterId, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT user_accounts.id
            FROM user_accounts
            INNER JOIN characters ON characters.user_account_id = user_accounts.id
            WHERE user_accounts.discord_user_id = @discord_user_id
              AND characters.id = @character_id
              AND characters.character_status = 'active'
            LIMIT 1;
            """;
        AddParameter(command, "@discord_user_id", discordUserId.ToString());
        AddParameter(command, "@character_id", characterId);
        object? raw = await command.ExecuteScalarAsync(cancellationToken);
        return raw is null || raw is DBNull ? null : Convert.ToUInt64(raw);
    }

    private static UserProfileDto ReadProfile(DbDataReader reader)
    {
        return new UserProfileDto(
            Convert.ToUInt64(reader["user_account_id"]),
            Convert.ToUInt64(reader["discord_user_id"]),
            Convert.ToString(reader["username"]) ?? string.Empty,
            reader["public_nickname"] is DBNull ? null : Convert.ToString(reader["public_nickname"]),
            reader["avatar_url"] is DBNull ? null : Convert.ToString(reader["avatar_url"]),
            Convert.ToString(reader["account_status"]) ?? "active",
            Convert.ToUInt32(reader["max_roster_slots"]),
            Convert.ToUInt32(reader["character_count"]),
            reader["active_character_id"] is DBNull ? null : Convert.ToUInt64(reader["active_character_id"]),
            reader["active_character_name"] is DBNull ? null : Convert.ToString(reader["active_character_name"]),
            Convert.ToInt64(reader["gold"]),
            Convert.ToInt64(reader["premium_currency"]),
            Convert.ToDateTime(reader["registered_at"]),
            reader["last_interaction_at"] is DBNull ? null : Convert.ToDateTime(reader["last_interaction_at"]));
    }

    private static UserWalletDto ReadWallet(DbDataReader reader)
    {
        return new UserWalletDto(
            Convert.ToUInt64(reader["user_account_id"]),
            Convert.ToInt64(reader["gold"]),
            Convert.ToInt64(reader["premium_currency"]),
            Convert.ToDateTime(reader["updated_at"]));
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
