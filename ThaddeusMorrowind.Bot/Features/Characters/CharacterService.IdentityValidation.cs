using System.Data.Common;

namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed partial class CharacterService
{
    private async Task<bool> CharacterNameExistsAsync(
        DbConnection connection,
        ulong userAccountId,
        string name,
        ulong? excludeCharacterId,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();

        command.CommandText =
            "SELECT COUNT(*) " +
            "FROM characters " +
            "WHERE user_account_id = @user_account_id " +
            "AND character_status = 'active' " +
            "AND LOWER(TRIM(name)) = LOWER(TRIM(@name)) " +
            "AND (@exclude_character_id IS NULL OR id <> @exclude_character_id);";

        AddParameter(command, "@user_account_id", userAccountId);
        AddParameter(command, "@name", name);
        AddParameter(command, "@exclude_character_id", excludeCharacterId);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToUInt64(result ?? 0) > 0;
    }

    private async Task<bool> CharacterNicknameExistsAsync(
        DbConnection connection,
        ulong userAccountId,
        string nickname,
        ulong? excludeCharacterId,
        CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();

        command.CommandText =
            "SELECT COUNT(*) " +
            "FROM characters " +
            "WHERE user_account_id = @user_account_id " +
            "AND character_status = 'active' " +
            "AND nickname IS NOT NULL " +
            "AND TRIM(nickname) <> '' " +
            "AND LOWER(TRIM(nickname)) = LOWER(TRIM(@nickname)) " +
            "AND (@exclude_character_id IS NULL OR id <> @exclude_character_id);";

        AddParameter(command, "@user_account_id", userAccountId);
        AddParameter(command, "@nickname", nickname);
        AddParameter(command, "@exclude_character_id", excludeCharacterId);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToUInt64(result ?? 0) > 0;
    }
}
