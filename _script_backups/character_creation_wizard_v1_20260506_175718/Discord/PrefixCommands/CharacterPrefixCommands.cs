using Discord.Commands;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.PrefixCommands;

[Group("personaje")]
[Alias("pj", "character")]
public sealed class CharacterPrefixCommands : ModuleBase<SocketCommandContext>
{
    private readonly ICharacterService _characterService;
    private readonly ICharacterCatalogService _catalogService;

    public CharacterPrefixCommands(
        ICharacterService characterService,
        ICharacterCatalogService catalogService)
    {
        _characterService = characterService;
        _catalogService = catalogService;
    }


    [Command("naciones")]
    public async Task NationsAsync()
    {
        IReadOnlyList<CharacterCatalogOptionDto> options = await _catalogService.GetNationsAsync();

        await ReplyAsync(
            embed: options.Count == 0
                ? CharacterCatalogViews.Missing("nation")
                : CharacterCatalogViews.List(
                    "🏳️ Naciones disponibles",
                    "Las naciones definen identidad, estilo narrativo y parte del crecimiento del personaje.",
                    options),
            components: options.Count == 0 ? null : CharacterCatalogComponents.CatalogMenu("nation", options));
    }

    [Command("roles")]
    public async Task RolesAsync()
    {
        IReadOnlyList<CharacterCatalogOptionDto> options = await _catalogService.GetRolesAsync();

        await ReplyAsync(
            embed: options.Count == 0
                ? CharacterCatalogViews.Missing("role")
                : CharacterCatalogViews.List(
                    "⚔️ Roles disponibles",
                    "Los roles definen el trabajo principal del personaje en combate y equipo.",
                    options),
            components: options.Count == 0 ? null : CharacterCatalogComponents.CatalogMenu("role", options));
    }

    [Command("profesiones")]
    public async Task ProfessionsAsync()
    {
        IReadOnlyList<CharacterCatalogOptionDto> options = await _catalogService.GetProfessionsAsync();

        await ReplyAsync(
            embed: options.Count == 0
                ? CharacterCatalogViews.Missing("profession")
                : CharacterCatalogViews.List(
                    "🧰 Profesiones disponibles",
                    "Las profesiones definen utilidad, exploración, recursos y eventos especiales.",
                    options),
            components: options.Count == 0 ? null : CharacterCatalogComponents.CatalogMenu("profession", options));
    }

    [Command("crear")]
    public async Task CreateAsync(string nombre, string nacion, string rol, string profesion, string? apodo = null)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.CreateAsync(
            Context.User.Id,
            new CharacterCreateRequestDto(nombre, apodo, null, nacion, rol, profesion));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Created(result.Data)
                : CharacterProfileViews.Result("⚠️ No se pudo crear", result.Message, false),
            components: result.Success && result.Data is not null ? CharacterProfileComponents.ForOwner(result.Data) : null);
    }

    [Command("lista")]
    public async Task ListAsync(string? archivados = null)
    {
        bool includeArchived = string.Equals(archivados, "archivados", StringComparison.OrdinalIgnoreCase)
            || string.Equals(archivados, "todos", StringComparison.OrdinalIgnoreCase);

        CharacterCommandResult<IReadOnlyList<CharacterListItemDto>> result = await _characterService.ListAsync(
            Context.User.Id,
            includeArchived);

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.List(result.Data, includeArchived)
                : CharacterProfileViews.Result("⚠️ No se pudo listar", result.Message, false));
    }

    [Command("ver")]
    public async Task ViewAsync(SocketUser? usuario = null, [Remainder] string? nombre = null)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            new CharacterLookupDto(null, nombre, usuario?.Id));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data)
                : CharacterProfileViews.Result("📭 Personaje no encontrado", result.Message, false),
            components: result.Success && result.Data is not null
                ? (result.Data.OwnerDiscordUserId == Context.User.Id ? CharacterProfileComponents.ForOwner(result.Data) : CharacterProfileComponents.ForPublic(result.Data))
                : null);
    }

    [Command("verid")]
    public async Task ViewByIdAsync(ulong personajeId)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            new CharacterLookupDto(personajeId, null, null));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data)
                : CharacterProfileViews.Result("📭 Personaje no encontrado", result.Message, false));
    }

    [Command("seleccionar")]
    public async Task SelectAsync([Remainder] string? nombre = null)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.SelectAsync(
            Context.User.Id,
            new CharacterLookupDto(null, nombre, null));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data, "⭐ Personaje activo")
                : CharacterProfileViews.Result("⚠️ No se pudo seleccionar", result.Message, false),
            components: result.Success && result.Data is not null ? CharacterProfileComponents.ForOwner(result.Data) : null);
    }

    [Command("seleccionarid")]
    public async Task SelectByIdAsync(ulong personajeId)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.SelectAsync(
            Context.User.Id,
            new CharacterLookupDto(personajeId, null, null));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data, "⭐ Personaje activo")
                : CharacterProfileViews.Result("⚠️ No se pudo seleccionar", result.Message, false));
    }

    [Command("editar")]
    public async Task EditAsync(ulong personajeId, string? nuevoNombre = null, string? nuevoApodo = null, string? nuevaImagenUrl = null)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.EditAsync(
            Context.User.Id,
            new CharacterEditRequestDto(personajeId, null, nuevoNombre, nuevoApodo, nuevaImagenUrl));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data, "✏️ Personaje editado")
                : CharacterProfileViews.Result("⚠️ No se pudo editar", result.Message, false));
    }

    [Command("archivar")]
    public async Task ArchiveAsync([Remainder] string? nombre = null)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.ArchiveAsync(
            Context.User.Id,
            new CharacterLookupDto(null, nombre, null));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data, "📦 Personaje archivado")
                : CharacterProfileViews.Result("⚠️ No se pudo archivar", result.Message, false));
    }

    [Command("restaurar")]
    public async Task RestoreAsync([Remainder] string? nombre = null)
    {
        bool isAdmin = Context.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator;

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.RestoreAsync(
            Context.User.Id,
            new CharacterLookupDto(null, nombre, null),
            allowAnyOwner: isAdmin);

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data, "♻️ Personaje restaurado")
                : CharacterProfileViews.Result("⚠️ No se pudo restaurar", result.Message, false));
    }

    [Command("stats")]
    public async Task StatsAsync(SocketUser? usuario = null, [Remainder] string? nombre = null)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            new CharacterLookupDto(null, nombre, usuario?.Id));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Stats(result.Data)
                : CharacterProfileViews.Result("📭 Stats no encontrados", result.Message, false));
    }

    [Command("equipo")]
    public async Task EquipmentAsync(SocketUser? usuario = null, [Remainder] string? nombre = null)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            new CharacterLookupDto(null, nombre, usuario?.Id));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Equipment(result.Data)
                : CharacterProfileViews.Result("📭 Equipo no encontrado", result.Message, false));
    }

    [Command("arbol")]
    [Alias("árbol")]
    public async Task TreeAsync(SocketUser? usuario = null, [Remainder] string? nombre = null)
    {
        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            new CharacterLookupDto(null, nombre, usuario?.Id));

        await ReplyAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.SkillTree(result.Data)
                : CharacterProfileViews.Result("📭 Árbol no encontrado", result.Message, false));
    }
}
