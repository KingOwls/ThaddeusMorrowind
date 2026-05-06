using Microsoft.EntityFrameworkCore;
using ThaddeusMorrowind.Bot.Common.Helpers;
using ThaddeusMorrowind.Bot.Data;
using ThaddeusMorrowind.Bot.Data.Entities;

namespace ThaddeusMorrowind.Bot.Features.Characters;

public sealed class CharacterService : ICharacterService
{
    private readonly GameDbContext _dbContext;

    public CharacterService(GameDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CharacterCatalogOptionDto>> GetNationOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Nations
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CharacterCatalogOptionDto(x.Id, x.NationKey, x.Name, x.ShortDescription, x.Description, x.IconUrl, x.BannerUrl, x.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CharacterCatalogOptionDto>> GetProfessionOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Professions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CharacterCatalogOptionDto(x.Id, x.ProfessionKey, x.Name, x.ShortDescription, x.Description, x.IconUrl, x.BannerUrl, x.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CharacterCatalogOptionDto>> GetRoleOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CharacterCatalogOptionDto(x.Id, x.RoleKey, x.Name, x.ShortDescription, x.Description, x.IconUrl, x.BannerUrl, x.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<CharacterCatalogOptionDto?> GetNationOptionAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Nations
            .AsNoTracking()
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new CharacterCatalogOptionDto(x.Id, x.NationKey, x.Name, x.ShortDescription, x.Description, x.IconUrl, x.BannerUrl, x.DisplayOrder))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CharacterCatalogOptionDto?> GetProfessionOptionAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Professions
            .AsNoTracking()
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new CharacterCatalogOptionDto(x.Id, x.ProfessionKey, x.Name, x.ShortDescription, x.Description, x.IconUrl, x.BannerUrl, x.DisplayOrder))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CharacterCatalogOptionDto?> GetRoleOptionAsync(ulong id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .Where(x => x.Id == id && x.IsActive)
            .Select(x => new CharacterCatalogOptionDto(x.Id, x.RoleKey, x.Name, x.ShortDescription, x.Description, x.IconUrl, x.BannerUrl, x.DisplayOrder))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CharacterCreateResult> CreateCharacterFromCatalogAsync(
        ulong discordUserId,
        string username,
        string? displayName,
        string characterName,
        string? nickname,
        ulong nationId,
        ulong professionId,
        ulong roleId,
        string? portraitUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(characterName))
        {
            return new CharacterCreateResult(false, "El nombre del personaje no puede estar vacío.", null);
        }

        if (characterName.Length > 80)
        {
            return new CharacterCreateResult(false, "El nombre del personaje no puede superar 80 caracteres.", null);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        UserAccount user = await GetOrCreateUserAsync(discordUserId, username, displayName, cancellationToken);

        string trimmedName = characterName.Trim();

        bool nameExists = await _dbContext.Characters.AnyAsync(
            x => x.UserAccountId == user.Id
                 && x.Name == trimmedName
                 && x.Status == "active",
            cancellationToken);

        if (nameExists)
        {
            return new CharacterCreateResult(false, "Ya tienes un personaje activo con ese nombre.", null);
        }

        Nation? nation = await _dbContext.Nations.FirstOrDefaultAsync(x => x.Id == nationId && x.IsActive, cancellationToken);
        Profession? profession = await _dbContext.Professions.FirstOrDefaultAsync(x => x.Id == professionId && x.IsActive, cancellationToken);
        Role? role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Id == roleId && x.IsActive, cancellationToken);

        if (nation is null)
        {
            return new CharacterCreateResult(false, "La nación seleccionada no está disponible.", null);
        }

        if (profession is null)
        {
            return new CharacterCreateResult(false, "La profesión seleccionada no está disponible.", null);
        }

        if (role is null)
        {
            return new CharacterCreateResult(false, "El rol seleccionado no está disponible.", null);
        }

        int currentCharacters = await _dbContext.Characters
            .CountAsync(x => x.UserAccountId == user.Id && x.Status == "active", cancellationToken);

        bool isFirstCharacter = currentCharacters == 0;

        Character character = new()
        {
            UserAccountId = user.Id,
            Name = trimmedName,
            Nickname = string.IsNullOrWhiteSpace(nickname) ? trimmedName : nickname.Trim(),
            PortraitUrl = string.IsNullOrWhiteSpace(portraitUrl) ? null : portraitUrl.Trim(),
            ThumbnailUrl = string.IsNullOrWhiteSpace(portraitUrl) ? null : portraitUrl.Trim(),
            NationId = nation.Id,
            ProfessionId = profession.Id,
            RoleId = role.Id,
            Level = 1,
            Experience = 0,
            IsMainCharacter = isFirstCharacter,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Characters.Add(character);
        await _dbContext.SaveChangesAsync(cancellationToken);

        List<StatType> statTypes = await _dbContext.StatTypes
            .Where(x => x.CanBeBase && x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (StatType statType in statTypes)
        {
            _dbContext.CharacterStats.Add(new CharacterStat
            {
                CharacterId = character.Id,
                StatTypeId = statType.Id,
                BaseValue = statType.DefaultBase,
                ExtraValue = 0,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (isFirstCharacter)
        {
            _dbContext.UserActiveCharacters.Add(new UserActiveCharacter
            {
                UserAccountId = user.Id,
                CharacterId = character.Id,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        CharacterDetailDto? detail = await GetCharacterDetailByIdAsync(user.Id, character.Id, cancellationToken);

        return new CharacterCreateResult(true, "Personaje creado correctamente.", detail);
    }

    public async Task<CharacterCreateResult> CreateCharacterAsync(
        ulong discordUserId,
        string username,
        string? displayName,
        string characterName,
        string nationInput,
        string professionInput,
        string roleInput,
        CancellationToken cancellationToken = default)
    {
        string nationKey = GameKeyNormalizer.NormalizeKey(nationInput);
        string professionKey = GameKeyNormalizer.NormalizeKey(professionInput);
        string roleKey = GameKeyNormalizer.NormalizeKey(roleInput);

        Nation? nation = await _dbContext.Nations.FirstOrDefaultAsync(x => x.IsActive && x.NationKey == nationKey, cancellationToken);
        Profession? profession = await _dbContext.Professions.FirstOrDefaultAsync(x => x.IsActive && x.ProfessionKey == professionKey, cancellationToken);
        Role? role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.IsActive && x.RoleKey == roleKey, cancellationToken);

        if (nation is null)
        {
            return new CharacterCreateResult(false, $"No encontré la nación `{nationInput}`.", null);
        }

        if (profession is null)
        {
            return new CharacterCreateResult(false, $"No encontré la profesión `{professionInput}`.", null);
        }

        if (role is null)
        {
            return new CharacterCreateResult(false, $"No encontré el rol `{roleInput}`.", null);
        }

        return await CreateCharacterFromCatalogAsync(
            discordUserId,
            username,
            displayName,
            characterName,
            characterName,
            nation.Id,
            profession.Id,
            role.Id,
            null,
            cancellationToken);
    }

    public async Task<CharacterCreateResult> UpdateCharacterAsync(
        ulong discordUserId,
        ulong characterId,
        string? newName,
        string? newNickname,
        string? newPortraitUrl,
        bool clearPortrait = false,
        CancellationToken cancellationToken = default)
    {
        UserAccount? user = await GetUserByDiscordIdAsync(discordUserId, cancellationToken);

        if (user is null)
        {
            return new CharacterCreateResult(false, "No estás registrado.", null);
        }

        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(
                x => x.Id == characterId
                     && x.UserAccountId == user.Id
                     && x.Status == "active",
                cancellationToken);

        if (character is null)
        {
            return new CharacterCreateResult(false, "No encontré ese personaje entre tus personajes activos.", null);
        }

        bool changed = false;

        if (!string.IsNullOrWhiteSpace(newName))
        {
            string trimmedName = newName.Trim();

            if (trimmedName.Length > 80)
            {
                return new CharacterCreateResult(false, "El nombre no puede superar 80 caracteres.", null);
            }

            bool duplicateName = await _dbContext.Characters.AnyAsync(
                x => x.UserAccountId == user.Id
                     && x.Id != character.Id
                     && x.Name == trimmedName
                     && x.Status == "active",
                cancellationToken);

            if (duplicateName)
            {
                return new CharacterCreateResult(false, "Ya tienes otro personaje activo con ese nombre.", null);
            }

            if (character.Name != trimmedName)
            {
                character.Name = trimmedName;
                changed = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(newNickname))
        {
            string trimmedNickname = newNickname.Trim();

            if (trimmedNickname.Length > 80)
            {
                return new CharacterCreateResult(false, "El apodo no puede superar 80 caracteres.", null);
            }

            if (character.Nickname != trimmedNickname)
            {
                character.Nickname = trimmedNickname;
                changed = true;
            }
        }

        if (clearPortrait)
        {
            if (character.PortraitUrl is not null || character.ThumbnailUrl is not null)
            {
                character.PortraitUrl = null;
                character.ThumbnailUrl = null;
                changed = true;
            }
        }
        else if (!string.IsNullOrWhiteSpace(newPortraitUrl))
        {
            string trimmedPortraitUrl = newPortraitUrl.Trim();

            if (character.PortraitUrl != trimmedPortraitUrl || character.ThumbnailUrl != trimmedPortraitUrl)
            {
                character.PortraitUrl = trimmedPortraitUrl;
                character.ThumbnailUrl = trimmedPortraitUrl;
                changed = true;
            }
        }

        if (!changed)
        {
            return new CharacterCreateResult(false, "No se recibió ningún cambio para aplicar.", null);
        }

        character.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        CharacterDetailDto? detail = await GetCharacterDetailByIdAsync(user.Id, character.Id, cancellationToken);

        return new CharacterCreateResult(true, "Personaje actualizado correctamente.", detail);
    }

    public async Task<CharacterDeleteResult> AdminDeleteCharacterAsync(
        ulong characterId,
        ulong adminDiscordUserId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(
                x => x.Id == characterId
                     && x.Status == "active",
                cancellationToken);

        if (character is null)
        {
            return new CharacterDeleteResult(false, "No encontré un personaje activo con ese ID.", null, null);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        character.Status = "deleted";
        character.DeletedAt = DateTime.UtcNow;
        character.DeletedByDiscordUserId = adminDiscordUserId.ToString();
        character.DeleteReason = string.IsNullOrWhiteSpace(reason) ? "Eliminado por administrador." : reason.Trim();
        character.UpdatedAt = DateTime.UtcNow;

        UserActiveCharacter? active = await _dbContext.UserActiveCharacters
            .FirstOrDefaultAsync(x => x.UserAccountId == character.UserAccountId, cancellationToken);

        if (active is not null && active.CharacterId == character.Id)
        {
            Character? replacement = await _dbContext.Characters
                .Where(x => x.UserAccountId == character.UserAccountId
                            && x.Id != character.Id
                            && x.Status == "active")
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (replacement is null)
            {
                _dbContext.UserActiveCharacters.Remove(active);
            }
            else
            {
                active.CharacterId = replacement.Id;
                active.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CharacterDeleteResult(true, "Personaje eliminado correctamente.", character.Id, character.Name);
    }

    public async Task<IReadOnlyList<CharacterSummaryDto>> ListCharactersAsync(
        ulong discordUserId,
        CancellationToken cancellationToken = default)
    {
        UserAccount? user = await GetUserByDiscordIdAsync(discordUserId, cancellationToken);

        if (user is null)
        {
            return Array.Empty<CharacterSummaryDto>();
        }

        ulong? activeCharacterId = await _dbContext.UserActiveCharacters
            .Where(x => x.UserAccountId == user.Id)
            .Select(x => (ulong?)x.CharacterId)
            .FirstOrDefaultAsync(cancellationToken);

        return await _dbContext.Characters
            .AsNoTracking()
            .Include(x => x.Nation)
            .Include(x => x.Profession)
            .Include(x => x.Role)
            .Where(x => x.UserAccountId == user.Id && x.Status == "active")
            .OrderBy(x => x.Id)
            .Select(x => new CharacterSummaryDto(
                x.Id,
                x.Name,
                x.Nickname,
                x.Level,
                x.Experience,
                x.Nation!.Name,
                x.Profession!.Name,
                x.Role!.Name,
                x.ThumbnailUrl ?? x.PortraitUrl,
                x.IsMainCharacter,
                activeCharacterId == x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<CharacterDetailDto?> GetCharacterAsync(
        ulong discordUserId,
        ulong? characterId,
        CancellationToken cancellationToken = default)
    {
        UserAccount? user = await GetUserByDiscordIdAsync(discordUserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        ulong? selectedCharacterId = characterId;

        if (selectedCharacterId is null)
        {
            selectedCharacterId = await _dbContext.UserActiveCharacters
                .Where(x => x.UserAccountId == user.Id)
                .Select(x => (ulong?)x.CharacterId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (selectedCharacterId is null)
        {
            return null;
        }

        return await GetCharacterDetailByIdAsync(user.Id, selectedCharacterId.Value, cancellationToken);
    }

    public async Task<CharacterDetailDto?> SelectCharacterAsync(
        ulong discordUserId,
        ulong characterId,
        CancellationToken cancellationToken = default)
    {
        UserAccount? user = await GetUserByDiscordIdAsync(discordUserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        bool ownsCharacter = await _dbContext.Characters.AnyAsync(
            x => x.Id == characterId
                 && x.UserAccountId == user.Id
                 && x.Status == "active",
            cancellationToken);

        if (!ownsCharacter)
        {
            return null;
        }

        UserActiveCharacter? active = await _dbContext.UserActiveCharacters
            .FirstOrDefaultAsync(x => x.UserAccountId == user.Id, cancellationToken);

        if (active is null)
        {
            _dbContext.UserActiveCharacters.Add(new UserActiveCharacter
            {
                UserAccountId = user.Id,
                CharacterId = characterId,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            active.CharacterId = characterId;
            active.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetCharacterDetailByIdAsync(user.Id, characterId, cancellationToken);
    }

    private async Task<UserAccount> GetOrCreateUserAsync(
        ulong discordUserId,
        string username,
        string? displayName,
        CancellationToken cancellationToken)
    {
        string discordUserIdText = discordUserId.ToString();

        UserAccount? user = await _dbContext.UserAccounts
            .FirstOrDefaultAsync(x => x.DiscordUserId == discordUserIdText, cancellationToken);

        if (user is not null)
        {
            return user;
        }

        user = new UserAccount
        {
            DiscordUserId = discordUserIdText,
            Username = username,
            DisplayName = displayName,
            Status = "active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.UserAccounts.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.UserWallets.Add(new UserWallet
        {
            UserAccountId = user.Id,
            CurrencyKey = "gold",
            Amount = 0,
            UpdatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    private async Task<UserAccount?> GetUserByDiscordIdAsync(
        ulong discordUserId,
        CancellationToken cancellationToken)
    {
        string discordUserIdText = discordUserId.ToString();

        return await _dbContext.UserAccounts
            .FirstOrDefaultAsync(x => x.DiscordUserId == discordUserIdText, cancellationToken);
    }

    private async Task<CharacterDetailDto?> GetCharacterDetailByIdAsync(
        ulong userAccountId,
        ulong characterId,
        CancellationToken cancellationToken)
    {
        Character? character = await _dbContext.Characters
            .AsNoTracking()
            .Include(x => x.Nation)
            .Include(x => x.Profession)
            .Include(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Id == characterId
                     && x.UserAccountId == userAccountId
                     && x.Status == "active",
                cancellationToken);

        if (character is null)
        {
            return null;
        }

        ulong? activeCharacterId = await _dbContext.UserActiveCharacters
            .Where(x => x.UserAccountId == userAccountId)
            .Select(x => (ulong?)x.CharacterId)
            .FirstOrDefaultAsync(cancellationToken);

        List<CharacterStatDto> stats = await _dbContext.CharacterStats
            .AsNoTracking()
            .Include(x => x.StatType)
            .Where(x => x.CharacterId == characterId)
            .OrderBy(x => x.StatType!.Id)
            .Select(x => new CharacterStatDto(
                x.StatType!.StatKey,
                x.StatType.Name,
                x.StatType.ValueKind,
                x.BaseValue,
                x.ExtraValue,
                x.BaseValue + x.ExtraValue))
            .ToListAsync(cancellationToken);

        return new CharacterDetailDto(
            character.Id,
            character.Name,
            character.Nickname,
            character.Level,
            character.Experience,
            character.Nation!.Name,
            character.Profession!.Name,
            character.Role!.Name,
            character.Nation.BannerUrl,
            character.Profession.BannerUrl,
            character.Role.BannerUrl,
            character.PortraitUrl,
            character.ThumbnailUrl ?? character.PortraitUrl,
            null,
            character.IsMainCharacter,
            activeCharacterId == character.Id,
            stats);
    }
}
