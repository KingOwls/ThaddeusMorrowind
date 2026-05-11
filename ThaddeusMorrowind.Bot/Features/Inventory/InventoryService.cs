using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;
using ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Inventory;

public sealed class InventoryService : IInventoryService
{
    private readonly GameDbContext _dbContext;

    public InventoryService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryCommandResult<IReadOnlyList<InventoryItemDto>>> GetUserInventoryAsync(ulong discordUserId, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        ulong? userAccountId = await GetUserAccountIdAsync(connection, discordUserId, cancellationToken);

        if (userAccountId is null)
        {
            return InventoryCommandResult<IReadOnlyList<InventoryItemDto>>.Fail("No tienes cuenta registrada. Usa `/registro` primero.");
        }

        IReadOnlyList<InventoryItemDto> items = await LoadInventoryItemsAsync(
            connection,
            "item_instances.owner_user_account_id = @user_account_id AND item_instances.location = 'user_inventory'",
            cancellationToken,
            ("@user_account_id", userAccountId.Value));

        return InventoryCommandResult<IReadOnlyList<InventoryItemDto>>.Ok(items);
    }

    public async Task<InventoryCommandResult<IReadOnlyList<InventoryItemDto>>> GetCharacterInventoryAsync(ulong discordUserId, ulong? characterId, string? characterName, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        CharacterRef? character = await ResolveOwnedCharacterAsync(connection, discordUserId, characterId, characterName, cancellationToken);

        if (character is null)
        {
            return InventoryCommandResult<IReadOnlyList<InventoryItemDto>>.Fail("No encontré ese personaje en tu cuenta.");
        }

        IReadOnlyList<InventoryItemDto> items = await LoadInventoryItemsAsync(
            connection,
            "item_instances.owner_character_id = @character_id AND item_instances.location IN ('character_inventory', 'equipped')",
            cancellationToken,
            ("@character_id", character.CharacterId));

        return InventoryCommandResult<IReadOnlyList<InventoryItemDto>>.Ok(items);
    }

    public async Task<InventoryCommandResult<EquipmentSummaryDto>> GetEquipmentAsync(ulong requesterDiscordUserId, ulong? ownerDiscordUserId, ulong? characterId, string? characterName, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        ulong ownerId = ownerDiscordUserId ?? requesterDiscordUserId;

        CharacterRef? character = await ResolveCharacterForViewAsync(connection, ownerId, characterId, characterName, cancellationToken);

        if (character is null)
        {
            return InventoryCommandResult<EquipmentSummaryDto>.Fail("No encontré ese personaje.");
        }

        EquipmentSummaryDto summary = await LoadEquipmentAsync(connection, character.CharacterId, cancellationToken);
        return InventoryCommandResult<EquipmentSummaryDto>.Ok(summary);
    }

    public async Task<InventoryCommandResult<EquipmentSummaryDto>> EquipAsync(ulong discordUserId, ulong characterId, ulong itemInstanceId, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        CharacterRef? character = await ResolveOwnedCharacterAsync(connection, discordUserId, characterId, null, cancellationToken);

        if (character is null)
        {
            return InventoryCommandResult<EquipmentSummaryDto>.Fail("No encontré ese personaje en tu cuenta.");
        }

        ItemEquipRef? item = await LoadItemForEquipAsync(connection, character.UserAccountId, character.CharacterId, itemInstanceId, cancellationToken);

        if (item is null)
        {
            return InventoryCommandResult<EquipmentSummaryDto>.Fail("No encontré ese item en tu inventario o no te pertenece.");
        }

        string? slotKey = ResolveSlotKey(item);

        if (slotKey is null)
        {
            return InventoryCommandResult<EquipmentSummaryDto>.Fail($"El item **{item.Name}** no es equipable todavía.");
        }

        ulong? slotId = await GetSlotIdAsync(connection, slotKey, cancellationToken);

        if (slotId is null)
        {
            return InventoryCommandResult<EquipmentSummaryDto>.Fail($"El slot `{slotKey}` no existe en la base de datos.");
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await UnequipExistingInSlotAsync(connection, transaction, character.CharacterId, slotId.Value, cancellationToken);
            await UpsertEquipmentSlotAsync(connection, transaction, character.CharacterId, slotId.Value, itemInstanceId, cancellationToken);
            await UpdateItemLocationAsync(connection, transaction, itemInstanceId, character.CharacterId, "equipped", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        EquipmentSummaryDto summary = await LoadEquipmentAsync(connection, character.CharacterId, cancellationToken);
        return InventoryCommandResult<EquipmentSummaryDto>.Ok(summary, $"Equipaste **{item.Name}** en `{slotKey}`.");
    }

    public async Task<InventoryCommandResult<EquipmentSummaryDto>> UnequipAsync(ulong discordUserId, ulong characterId, string slotKey, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        CharacterRef? character = await ResolveOwnedCharacterAsync(connection, discordUserId, characterId, null, cancellationToken);

        if (character is null)
        {
            return InventoryCommandResult<EquipmentSummaryDto>.Fail("No encontré ese personaje en tu cuenta.");
        }

        ulong? slotId = await GetSlotIdAsync(connection, slotKey, cancellationToken);

        if (slotId is null)
        {
            return InventoryCommandResult<EquipmentSummaryDto>.Fail($"El slot `{slotKey}` no existe.");
        }

        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await UnequipExistingInSlotAsync(connection, transaction, character.CharacterId, slotId.Value, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        EquipmentSummaryDto summary = await LoadEquipmentAsync(connection, character.CharacterId, cancellationToken);
        return InventoryCommandResult<EquipmentSummaryDto>.Ok(summary, $"Slot `{slotKey}` retirado.");
    }

    public async Task<InventoryCommandResult<DebugGiveItemDto>> GiveItemAsync(ulong targetDiscordUserId, string itemKey, int quantity, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await OpenConnectionAsync(cancellationToken);
        ulong? userAccountId = await GetUserAccountIdAsync(connection, targetDiscordUserId, cancellationToken);

        if (userAccountId is null)
        {
            return InventoryCommandResult<DebugGiveItemDto>.Fail("El usuario objetivo no tiene cuenta registrada.");
        }

        ItemTemplateRef? template = await GetItemTemplateAsync(connection, itemKey, cancellationToken);

        if (template is null)
        {
            return InventoryCommandResult<DebugGiveItemDto>.Fail($"No encontré item_template con key `{itemKey}`.");
        }

        int finalQuantity = Math.Max(1, quantity);

        if (!template.IsStackable)
        {
            finalQuantity = 1;
        }

        await using DbCommand insert = connection.CreateCommand();
        insert.CommandText = @"
            INSERT INTO item_instances (
                item_template_id,
                owner_user_account_id,
                owner_character_id,
                location,
                quantity,
                level,
                refinement,
                quality_rank,
                is_locked
            )
            VALUES (
                @template_id,
                @user_account_id,
                NULL,
                'user_inventory',
                @quantity,
                0,
                0,
                'normal',
                FALSE
            );
            SELECT LAST_INSERT_ID();
            ";

        AddParameter(insert, "@template_id", template.ItemTemplateId);
        AddParameter(insert, "@user_account_id", userAccountId.Value);
        AddParameter(insert, "@quantity", finalQuantity);

        ulong itemInstanceId = Convert.ToUInt64(await insert.ExecuteScalarAsync(cancellationToken));
        string username = await GetUsernameByAccountIdAsync(connection, userAccountId.Value, cancellationToken);

        return InventoryCommandResult<DebugGiveItemDto>.Ok(new DebugGiveItemDto(itemInstanceId, template.Name, finalQuantity, username), "Item otorgado.");
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

    private async Task<ulong?> GetUserAccountIdAsync(DbConnection connection, ulong discordUserId, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id
            FROM user_accounts
            WHERE discord_user_id = @discord_user_id
            LIMIT 1;
            ";

        AddParameter(command, "@discord_user_id", discordUserId);
        object? result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null || result is DBNull ? null : Convert.ToUInt64(result);
    }

    private async Task<string> GetUsernameByAccountIdAsync(DbConnection connection, ulong userAccountId, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT username
            FROM user_accounts
            WHERE id = @id
            LIMIT 1;
            ";

        AddParameter(command, "@id", userAccountId);

        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? "Usuario";
    }

    private async Task<CharacterRef?> ResolveOwnedCharacterAsync(DbConnection connection, ulong discordUserId, ulong? characterId, string? characterName, CancellationToken cancellationToken)
    {
        ulong? userAccountId = await GetUserAccountIdAsync(connection, discordUserId, cancellationToken);

        if (userAccountId is null)
        {
            return null;
        }

        return await ResolveCharacterAsync(connection, userAccountId.Value, characterId, characterName, true, cancellationToken);
    }

    private async Task<CharacterRef?> ResolveCharacterForViewAsync(DbConnection connection, ulong ownerDiscordUserId, ulong? characterId, string? characterName, CancellationToken cancellationToken)
    {
        ulong? userAccountId = await GetUserAccountIdAsync(connection, ownerDiscordUserId, cancellationToken);

        if (userAccountId is null)
        {
            return null;
        }

        return await ResolveCharacterAsync(connection, userAccountId.Value, characterId, characterName, true, cancellationToken);
    }

    private async Task<CharacterRef?> ResolveCharacterAsync(DbConnection connection, ulong userAccountId, ulong? characterId, string? characterName, bool onlyActive, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();

        if (characterId is not null && characterId.Value > 0)
        {
            command.CommandText = @"
                SELECT id, user_account_id, name, image_url
                FROM characters
                WHERE id = @character_id
                  AND user_account_id = @user_account_id
                  AND (@only_active = FALSE OR character_status = 'active')
                LIMIT 1;
                ";

            AddParameter(command, "@character_id", characterId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(characterName))
        {
            command.CommandText = @"
                SELECT id, user_account_id, name, image_url
                FROM characters
                WHERE user_account_id = @user_account_id
                  AND LOWER(TRIM(name)) = LOWER(TRIM(@name))
                  AND (@only_active = FALSE OR character_status = 'active')
                ORDER BY id DESC
                LIMIT 1;
                ";

            AddParameter(command, "@name", characterName.Trim());
        }
        else
        {
            command.CommandText = @"
                SELECT characters.id, characters.user_account_id, characters.name, characters.image_url
                FROM characters
                LEFT JOIN user_active_characters
                    ON user_active_characters.character_id = characters.id
                   AND user_active_characters.user_account_id = characters.user_account_id
                WHERE characters.user_account_id = @user_account_id
                  AND (@only_active = FALSE OR characters.character_status = 'active')
                ORDER BY
                    CASE WHEN user_active_characters.character_id IS NULL THEN 1 ELSE 0 END,
                    characters.id DESC
                LIMIT 1;
                ";
        }

        AddParameter(command, "@user_account_id", userAccountId);
        AddParameter(command, "@only_active", onlyActive);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterRef(
            Convert.ToUInt64(reader["id"]),
            Convert.ToUInt64(reader["user_account_id"]),
            Convert.ToString(reader["name"]) ?? "Personaje",
            reader["image_url"] is DBNull ? null : Convert.ToString(reader["image_url"]));
    }

    private async Task<IReadOnlyList<InventoryItemDto>> LoadInventoryItemsAsync(DbConnection connection, string whereClause, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT
                item_instances.id AS item_instance_id,
                item_templates.item_key,
                item_templates.name,
                item_categories.category_key,
                item_categories.name AS category_name,
                item_templates.item_subtype,
                item_rarities.name AS rarity_name,
                item_rarities.stars,
                item_instances.quantity,
                item_instances.location,
                item_instances.level,
                item_instances.refinement,
                item_instances.quality_rank,
                item_instances.is_locked,
                item_templates.icon_url,
                item_templates.short_description
            FROM item_instances
            INNER JOIN item_templates
                ON item_templates.id = item_instances.item_template_id
            INNER JOIN item_categories
                ON item_categories.id = item_templates.category_id
            LEFT JOIN item_rarities
                ON item_rarities.id = item_templates.rarity_id
            WHERE {whereClause}
            ORDER BY
                item_categories.display_order,
                item_templates.display_order,
                item_instances.id DESC;
            ";

        foreach ((string name, object value) in parameters)
        {
            AddParameter(command, name, value);
        }

        List<InventoryItemDto> items = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadInventoryItem(reader));
        }

        return items;
    }

    private async Task<EquipmentSummaryDto> LoadEquipmentAsync(DbConnection connection, ulong characterId, CancellationToken cancellationToken)
    {
        CharacterRef? character = await ResolveCharacterByIdAnyOwnerAsync(connection, characterId, cancellationToken);

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                equipment_slots.slot_key,
                equipment_slots.name AS slot_name,
                equipment_slots.slot_group,
                equipment_slots.counts_for_artifact_set,
                item_instances.id AS item_instance_id,
                item_templates.item_key,
                item_templates.name,
                item_categories.category_key,
                item_categories.name AS category_name,
                item_templates.item_subtype,
                item_rarities.name AS rarity_name,
                item_rarities.stars,
                item_instances.quantity,
                item_instances.location,
                item_instances.level,
                item_instances.refinement,
                item_instances.quality_rank,
                item_instances.is_locked,
                item_templates.icon_url,
                item_templates.short_description
            FROM equipment_slots
            LEFT JOIN character_equipment_slots
                ON character_equipment_slots.slot_id = equipment_slots.id
               AND character_equipment_slots.character_id = @character_id
            LEFT JOIN item_instances
                ON item_instances.id = character_equipment_slots.item_instance_id
            LEFT JOIN item_templates
                ON item_templates.id = item_instances.item_template_id
            LEFT JOIN item_categories
                ON item_categories.id = item_templates.category_id
            LEFT JOIN item_rarities
                ON item_rarities.id = item_templates.rarity_id
            WHERE equipment_slots.is_active = TRUE
            ORDER BY equipment_slots.display_order;
            ";

        AddParameter(command, "@character_id", characterId);

        List<EquipmentSlotDto> slots = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            InventoryItemDto? item = reader["item_instance_id"] is DBNull ? null : ReadInventoryItem(reader);

            slots.Add(new EquipmentSlotDto(
                Convert.ToString(reader["slot_key"]) ?? string.Empty,
                Convert.ToString(reader["slot_name"]) ?? string.Empty,
                Convert.ToString(reader["slot_group"]) ?? string.Empty,
                Convert.ToBoolean(reader["counts_for_artifact_set"]),
                item));
        }

        return new EquipmentSummaryDto(character?.CharacterId ?? characterId, character?.Name ?? $"Personaje {characterId}", character?.ImageUrl, slots);
    }

    private async Task<CharacterRef?> ResolveCharacterByIdAnyOwnerAsync(DbConnection connection, ulong characterId, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, user_account_id, name, image_url
            FROM characters
            WHERE id = @character_id
            LIMIT 1;
            ";

        AddParameter(command, "@character_id", characterId);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CharacterRef(
            Convert.ToUInt64(reader["id"]),
            Convert.ToUInt64(reader["user_account_id"]),
            Convert.ToString(reader["name"]) ?? "Personaje",
            reader["image_url"] is DBNull ? null : Convert.ToString(reader["image_url"]));
    }

    private async Task<ItemTemplateRef?> GetItemTemplateAsync(DbConnection connection, string itemKey, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, name, is_stackable
            FROM item_templates
            WHERE item_key = @item_key
              AND is_active = TRUE
            LIMIT 1;
            ";

        AddParameter(command, "@item_key", itemKey);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ItemTemplateRef(Convert.ToUInt64(reader["id"]), Convert.ToString(reader["name"]) ?? itemKey, Convert.ToBoolean(reader["is_stackable"]));
    }

    private async Task<ItemEquipRef?> LoadItemForEquipAsync(DbConnection connection, ulong userAccountId, ulong characterId, ulong itemInstanceId, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT
                item_instances.id,
                item_templates.name,
                item_categories.category_key,
                item_templates.item_subtype
            FROM item_instances
            INNER JOIN item_templates
                ON item_templates.id = item_instances.item_template_id
            INNER JOIN item_categories
                ON item_categories.id = item_templates.category_id
            WHERE item_instances.id = @item_instance_id
              AND item_instances.owner_user_account_id = @user_account_id
              AND (
                    item_instances.owner_character_id IS NULL
                 OR item_instances.owner_character_id = @character_id
              )
            LIMIT 1;
            ";

        AddParameter(command, "@item_instance_id", itemInstanceId);
        AddParameter(command, "@user_account_id", userAccountId);
        AddParameter(command, "@character_id", characterId);

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ItemEquipRef(
            Convert.ToUInt64(reader["id"]),
            Convert.ToString(reader["name"]) ?? "Item",
            Convert.ToString(reader["category_key"]) ?? string.Empty,
            reader["item_subtype"] is DBNull ? null : Convert.ToString(reader["item_subtype"]));
    }

    private static string? ResolveSlotKey(ItemEquipRef item)
    {
        if (item.CategoryKey == "arma")
        {
            return "weapon";
        }

        return item.ItemSubtype switch
        {
            "artifact_flower" => "artifact_flower",
            "artifact_plume" => "artifact_plume",
            "artifact_clock" => "artifact_clock",
            "artifact_chalice" => "artifact_chalice",
            "artifact_hat" => "artifact_hat",
            "unique_tool" => "unique_tool",
            _ => null
        };
    }

    private async Task<ulong?> GetSlotIdAsync(DbConnection connection, string slotKey, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id
            FROM equipment_slots
            WHERE slot_key = @slot_key
              AND is_active = TRUE
            LIMIT 1;
            ";

        AddParameter(command, "@slot_key", slotKey);
        object? result = await command.ExecuteScalarAsync(cancellationToken);

        return result is null || result is DBNull ? null : Convert.ToUInt64(result);
    }

    private static async Task UnequipExistingInSlotAsync(DbConnection connection, DbTransaction transaction, ulong characterId, ulong slotId, CancellationToken cancellationToken)
    {
        ulong? oldItemId = null;

        await using (DbCommand find = connection.CreateCommand())
        {
            find.Transaction = transaction;
            find.CommandText = @"
                SELECT item_instance_id
                FROM character_equipment_slots
                WHERE character_id = @character_id
                  AND slot_id = @slot_id
                LIMIT 1;
                ";

            AddParameter(find, "@character_id", characterId);
            AddParameter(find, "@slot_id", slotId);

            object? result = await find.ExecuteScalarAsync(cancellationToken);

            if (result is not null && result is not DBNull)
            {
                oldItemId = Convert.ToUInt64(result);
            }
        }

        if (oldItemId is not null)
        {
            await using DbCommand updateOld = connection.CreateCommand();
            updateOld.Transaction = transaction;
            updateOld.CommandText = @"
                UPDATE item_instances
                SET location = 'character_inventory',
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @item_instance_id;
                ";

            AddParameter(updateOld, "@item_instance_id", oldItemId.Value);
            await updateOld.ExecuteNonQueryAsync(cancellationToken);
        }

        await using DbCommand delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = @"
            DELETE FROM character_equipment_slots
            WHERE character_id = @character_id
              AND slot_id = @slot_id;
            ";

        AddParameter(delete, "@character_id", characterId);
        AddParameter(delete, "@slot_id", slotId);
        await delete.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertEquipmentSlotAsync(DbConnection connection, DbTransaction transaction, ulong characterId, ulong slotId, ulong itemInstanceId, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            INSERT INTO character_equipment_slots (character_id, slot_id, item_instance_id)
            VALUES (@character_id, @slot_id, @item_instance_id)
            ON DUPLICATE KEY UPDATE
                item_instance_id = VALUES(item_instance_id),
                equipped_at = CURRENT_TIMESTAMP;
            ";

        AddParameter(command, "@character_id", characterId);
        AddParameter(command, "@slot_id", slotId);
        AddParameter(command, "@item_instance_id", itemInstanceId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdateItemLocationAsync(DbConnection connection, DbTransaction transaction, ulong itemInstanceId, ulong characterId, string location, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
            UPDATE item_instances
            SET owner_character_id = @character_id,
                location = @location,
                quantity = 1,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @item_instance_id;
            ";

        AddParameter(command, "@character_id", characterId);
        AddParameter(command, "@location", location);
        AddParameter(command, "@item_instance_id", itemInstanceId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static InventoryItemDto ReadInventoryItem(DbDataReader reader)
    {
        return new InventoryItemDto(
            Convert.ToUInt64(reader["item_instance_id"]),
            Convert.ToString(reader["item_key"]) ?? string.Empty,
            Convert.ToString(reader["name"]) ?? string.Empty,
            Convert.ToString(reader["category_key"]) ?? string.Empty,
            Convert.ToString(reader["category_name"]) ?? string.Empty,
            reader["item_subtype"] is DBNull ? null : Convert.ToString(reader["item_subtype"]),
            reader["rarity_name"] is DBNull ? null : Convert.ToString(reader["rarity_name"]),
            reader["stars"] is DBNull ? null : Convert.ToInt32(reader["stars"]),
            Convert.ToInt32(reader["quantity"]),
            Convert.ToString(reader["location"]) ?? string.Empty,
            Convert.ToInt32(reader["level"]),
            Convert.ToInt32(reader["refinement"]),
            Convert.ToString(reader["quality_rank"]) ?? "normal",
            Convert.ToBoolean(reader["is_locked"]),
            reader["icon_url"] is DBNull ? null : Convert.ToString(reader["icon_url"]),
            reader["short_description"] is DBNull ? null : Convert.ToString(reader["short_description"]));
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private sealed record CharacterRef(ulong CharacterId, ulong UserAccountId, string Name, string? ImageUrl);
    private sealed record ItemTemplateRef(ulong ItemTemplateId, string Name, bool IsStackable);
    private sealed record ItemEquipRef(ulong ItemInstanceId, string Name, string CategoryKey, string? ItemSubtype);
}
