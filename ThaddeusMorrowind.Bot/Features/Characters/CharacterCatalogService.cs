using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Data;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed class CharacterCatalogService : ICharacterCatalogService
{
    private readonly GameDbContext _dbContext;

    public CharacterCatalogService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IReadOnlyList<CharacterCatalogOptionDto>> GetNationsAsync(
        CancellationToken cancellationToken = default)
    {
        return LoadCatalogAsync(
            "nation",
            "nations",
            "nation_key",
            cancellationToken);
    }

    public Task<IReadOnlyList<CharacterCatalogOptionDto>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        return LoadCatalogAsync(
            "role",
            "roles",
            "role_key",
            cancellationToken);
    }

    public Task<IReadOnlyList<CharacterCatalogOptionDto>> GetProfessionsAsync(
        CancellationToken cancellationToken = default)
    {
        return LoadCatalogAsync(
            "profession",
            "professions",
            "profession_key",
            cancellationToken);
    }

    public async Task<CharacterCatalogOptionDto?> GetByKeyAsync(
        string catalogType,
        string key,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CharacterCatalogOptionDto> options = catalogType switch
        {
            "nation" or "nations" or "nacion" or "naciones" => await GetNationsAsync(cancellationToken),
            "role" or "roles" or "rol" => await GetRolesAsync(cancellationToken),
            "profession" or "professions" or "profesion" or "profesiones" => await GetProfessionsAsync(cancellationToken),
            _ => Array.Empty<CharacterCatalogOptionDto>()
        };

        return options.FirstOrDefault(option =>
            option.Key.Equals(key, StringComparison.OrdinalIgnoreCase) ||
            option.Name.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<CharacterCatalogOptionDto>> LoadCatalogAsync(
        string catalogType,
        string tableName,
        string keyColumn,
        CancellationToken cancellationToken)
    {
        DbConnection connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                id,
                {keyColumn} AS catalog_key,
                name,
                short_description,
                description,
                icon_url,
                banner_url,
                display_order
            FROM {tableName}
            WHERE is_active = TRUE
            ORDER BY display_order, id;
            """;

        List<CharacterCatalogOptionDto> options = new();

        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            options.Add(new CharacterCatalogOptionDto(
                catalogType,
                Convert.ToUInt64(reader["id"]),
                Convert.ToString(reader["catalog_key"]) ?? string.Empty,
                Convert.ToString(reader["name"]) ?? string.Empty,
                reader["short_description"] is DBNull ? null : Convert.ToString(reader["short_description"]),
                reader["description"] is DBNull ? null : Convert.ToString(reader["description"]),
                reader["icon_url"] is DBNull ? null : Convert.ToString(reader["icon_url"]),
                reader["banner_url"] is DBNull ? null : Convert.ToString(reader["banner_url"]),
                Convert.ToUInt32(reader["display_order"])));
        }

        return options;
    }
}
