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
    private readonly ICharacterCreationSessionStore _sessionStore;

    public CharacterPrefixCommands(
        ICharacterService characterService,
        ICharacterCatalogService catalogService,
        ICharacterCreationSessionStore sessionStore)
    {
        _characterService = characterService;
        _catalogService = catalogService;
        _sessionStore = sessionStore;
    }

    [Command("naciones")]
    public Task NationsAsync()
    {
        return ShowCatalogAsync(
            "nation",
            "🏳️ Naciones",
            "Las naciones definen identidad, afinidad narrativa y crecimiento base.",
            () => _catalogService.GetNationsAsync());
    }

    [Command("roles")]
    public Task RolesAsync()
    {
        return ShowCatalogAsync(
            "role",
            "⚔️ Roles",
            "Los roles definen función de combate y estilo principal.",
            () => _catalogService.GetRolesAsync());
    }

    [Command("profesiones")]
    public Task ProfessionsAsync()
    {
        return ShowCatalogAsync(
            "profession",
            "🧰 Profesiones",
            "Las profesiones definen utilidad, exploración y recursos.",
            () => _catalogService.GetProfessionsAsync());
    }

    [Command("crear")]
    public async Task CreateAsync(string nombre, string? apodo = null)
    {
        IReadOnlyList<CharacterCatalogOptionDto> nations = await _catalogService.GetNationsAsync();
        IReadOnlyList<CharacterCatalogOptionDto> roles = await _catalogService.GetRolesAsync();
        IReadOnlyList<CharacterCatalogOptionDto> professions = await _catalogService.GetProfessionsAsync();

        if (nations.Count == 0 || roles.Count == 0 || professions.Count == 0)
        {
            await ReplyAsync(embed: CharacterCreationWizardViews.CatalogMissing());
            return;
        }

        string? imageUrl = Context.Message.Attachments.FirstOrDefault()?.Url;

        CharacterCreationSessionDto session = _sessionStore.Create(
            Context.User.Id,
            nombre,
            apodo,
            imageUrl);

        await ReplyAsync(
            embed: CharacterCreationWizardViews.Wizard(
                session,
                nations[0],
                roles[0],
                professions[0],
                nations.Count,
                roles.Count,
                professions.Count),
            components: CharacterCreationWizardComponents.WizardButtons(session));
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
        string? imageUrl = nuevaImagenUrl ?? Context.Message.Attachments.FirstOrDefault()?.Url;

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.EditAsync(
            Context.User.Id,
            new CharacterEditRequestDto(personajeId, null, nuevoNombre, nuevoApodo, imageUrl));

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

    private async Task ShowCatalogAsync(
        string catalogType,
        string title,
        string intro,
        Func<Task<IReadOnlyList<CharacterCatalogOptionDto>>> loadOptions)
    {
        IReadOnlyList<CharacterCatalogOptionDto> options = await loadOptions();

        await ReplyAsync(
            embed: options.Count == 0
                ? CharacterCatalogBrowserViews.Empty(catalogType)
                : CharacterCatalogBrowserViews.Detail(
                    title,
                    intro,
                    options[0],
                    0,
                    options.Count),
            components: options.Count == 0
                ? null
                : CharacterCatalogBrowserComponents.BrowserButtons(catalogType, 0, options.Count));
    }
}
