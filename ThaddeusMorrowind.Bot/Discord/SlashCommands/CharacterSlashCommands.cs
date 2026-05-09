using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.SlashCommands;

[Group("personaje", "Gestiona personajes de Thaddeus Morrowind.")]
public sealed class CharacterSlashCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICharacterService _characterService;
    private readonly ICharacterCatalogService _catalogService;
    private readonly ICharacterCreationSessionStore _sessionStore;

    public CharacterSlashCommands(
        ICharacterService characterService,
        ICharacterCatalogService catalogService,
        ICharacterCreationSessionStore sessionStore)
    {
        _characterService = characterService;
        _catalogService = catalogService;
        _sessionStore = sessionStore;
    }

    [SlashCommand("naciones", "Muestra el catálogo visual navegable de naciones.")]
    public Task NationsAsync(bool publico = false)
    {
        return ShowCatalogAsync(
            "nation",
            "🏳️ Naciones",
            "Las naciones definen identidad, afinidad narrativa y crecimiento base.",
            () => _catalogService.GetNationsAsync(),
            publico);
    }

    [SlashCommand("roles", "Muestra el catálogo visual navegable de roles.")]
    public Task RolesAsync(bool publico = false)
    {
        return ShowCatalogAsync(
            "role",
            "⚔️ Roles",
            "Los roles definen función de combate y estilo principal.",
            () => _catalogService.GetRolesAsync(),
            publico);
    }

    [SlashCommand("profesiones", "Muestra el catálogo visual navegable de profesiones.")]
    public Task ProfessionsAsync(bool publico = false)
    {
        return ShowCatalogAsync(
            "profession",
            "🧰 Profesiones",
            "Las profesiones definen utilidad, exploración y recursos.",
            () => _catalogService.GetProfessionsAsync(),
            publico);
    }

    [SlashCommand("crear", "Inicia la creación visual de un personaje.")]
    public async Task CreateAsync(
        string nombre,
        string? apodo = null,
        IAttachment? imagen = null,
        string? imagen_url = null)
    {
        await DeferAsync(ephemeral: true);

        if (string.IsNullOrWhiteSpace(nombre))
        {
            await FollowupAsync(
                embed: CharacterProfileViews.Result(
                    "⚠️ Nombre inválido",
                    "El nombre del personaje no puede estar vacío.",
                    false),
                ephemeral: true);

            return;
        }

        IReadOnlyList<CharacterCatalogOptionDto> nations = await _catalogService.GetNationsAsync();
        IReadOnlyList<CharacterCatalogOptionDto> roles = await _catalogService.GetRolesAsync();
        IReadOnlyList<CharacterCatalogOptionDto> professions = await _catalogService.GetProfessionsAsync();

        if (nations.Count == 0 || roles.Count == 0 || professions.Count == 0)
        {
            await FollowupAsync(embed: CharacterCreationWizardViews.CatalogMissing(), ephemeral: true);
            return;
        }

        string? finalImageUrl = imagen?.Url;

        if (string.IsNullOrWhiteSpace(finalImageUrl))
        {
            finalImageUrl = imagen_url;
        }

        CharacterCreationSessionDto session = _sessionStore.Create(
            Context.User.Id,
            nombre,
            apodo,
            finalImageUrl);

        await FollowupAsync(
            embed: CharacterCreationWizardViews.Wizard(
                session,
                nations[0],
                roles[0],
                professions[0],
                nations.Count,
                roles.Count,
                professions.Count),
            components: CharacterCreationWizardComponents.WizardButtons(session),
            ephemeral: true);
    }

    [SlashCommand("lista", "Muestra tus personajes registrados.")]
    public async Task ListAsync(bool incluir_archivados = false)
    {
        await DeferAsync(ephemeral: true);

        CharacterCommandResult<IReadOnlyList<CharacterListItemDto>> result = await _characterService.ListAsync(
            Context.User.Id,
            incluir_archivados);

        if (!result.Success || result.Data is null)
        {
            await FollowupAsync(embed: CharacterProfileViews.Result("⚠️ No se pudo listar", result.Message, false), ephemeral: true);
            return;
        }

        await FollowupAsync(embed: CharacterProfileViews.List(result.Data, incluir_archivados), ephemeral: true);
    }

    [SlashCommand("ver", "Muestra la ficha pública de un personaje.")]
    public async Task ViewAsync(long personaje_id = 0, string? nombre = null, IUser? usuario = null)
    {
        await DeferAsync(ephemeral: false);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            BuildLookup(personaje_id, nombre, usuario));

        if (!result.Success || result.Data is null)
        {
            await FollowupAsync(embed: CharacterProfileViews.Result("📭 Personaje no encontrado", result.Message, false), ephemeral: false);
            return;
        }

        bool isOwner = result.Data.OwnerDiscordUserId == Context.User.Id;

        await FollowupAsync(
            embed: CharacterProfileViews.Profile(result.Data),
            components: isOwner ? CharacterProfileComponents.ForOwner(result.Data) : CharacterProfileComponents.ForPublic(result.Data),
            ephemeral: false);
    }

    [SlashCommand("seleccionar", "Marca un personaje tuyo como activo.")]
    public async Task SelectAsync(long personaje_id = 0, string? nombre = null)
    {
        await DeferAsync(ephemeral: true);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.SelectAsync(
            Context.User.Id,
            BuildLookup(personaje_id, nombre, null));

        if (!result.Success || result.Data is null)
        {
            await FollowupAsync(embed: CharacterProfileViews.Result("⚠️ No se pudo seleccionar", result.Message, false), ephemeral: true);
            return;
        }

        await FollowupAsync(
            embed: CharacterProfileViews.Profile(result.Data, "⭐ Personaje activo"),
            components: CharacterProfileComponents.ForOwner(result.Data),
            ephemeral: true);
    }

    [SlashCommand("editar", "Edita nombre, apodo o imagen de un personaje tuyo.")]
    public async Task EditAsync(
        long personaje_id = 0,
        string? nombre_actual = null,
        string? nuevo_nombre = null,
        string? nuevo_apodo = null,
        IAttachment? nueva_imagen = null,
        string? nueva_imagen_url = null)
    {
        await DeferAsync(ephemeral: true);

        string? finalImageUrl = nueva_imagen?.Url;

        if (string.IsNullOrWhiteSpace(finalImageUrl))
        {
            finalImageUrl = nueva_imagen_url;
        }

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.EditAsync(
            Context.User.Id,
            new CharacterEditRequestDto(
                personaje_id > 0 ? (ulong)personaje_id : null,
                nombre_actual,
                nuevo_nombre,
                nuevo_apodo,
                finalImageUrl));

        if (!result.Success || result.Data is null)
        {
            await FollowupAsync(embed: CharacterProfileViews.Result("⚠️ No se pudo editar", result.Message, false), ephemeral: true);
            return;
        }

        await FollowupAsync(
            embed: CharacterProfileViews.Profile(result.Data, "✏️ Personaje editado"),
            components: CharacterProfileComponents.ForOwner(result.Data),
            ephemeral: true);
    }

    [SlashCommand("archivar", "Archiva un personaje tuyo sin borrar datos.")]
    public async Task ArchiveAsync(long personaje_id = 0, string? nombre = null)
    {
        await DeferAsync(ephemeral: true);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.ArchiveAsync(
            Context.User.Id,
            BuildLookup(personaje_id, nombre, null));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data, "📦 Personaje archivado")
                : CharacterProfileViews.Result("⚠️ No se pudo archivar", result.Message, false),
            ephemeral: true);
    }

    [SlashCommand("restaurar", "Restaura un personaje archivado. Admin puede restaurar por ID.")]
    public async Task RestoreAsync(long personaje_id = 0, string? nombre = null)
    {
        await DeferAsync(ephemeral: true);

        bool isAdmin = Context.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator;

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.RestoreAsync(
            Context.User.Id,
            BuildLookup(personaje_id, nombre, null),
            allowAnyOwner: isAdmin);

        if (!result.Success || result.Data is null)
        {
            await FollowupAsync(embed: CharacterProfileViews.Result("⚠️ No se pudo restaurar", result.Message, false), ephemeral: true);
            return;
        }

        await FollowupAsync(
            embed: CharacterProfileViews.Profile(result.Data, "♻️ Personaje restaurado"),
            components: result.Data.OwnerDiscordUserId == Context.User.Id ? CharacterProfileComponents.ForOwner(result.Data) : CharacterProfileComponents.ForPublic(result.Data),
            ephemeral: true);
    }

    [SlashCommand("stats", "Muestra estadísticas detalladas de un personaje.")]
    public async Task StatsAsync(long personaje_id = 0, string? nombre = null, IUser? usuario = null)
    {
        await DeferAsync(ephemeral: false);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            BuildLookup(personaje_id, nombre, usuario));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Stats(result.Data)
                : CharacterProfileViews.Result("📭 Stats no encontrados", result.Message, false),
            ephemeral: false);
    }

    [SlashCommand("equipo", "Muestra arma, artefactos y ArtUnic del personaje.")]
    public async Task EquipmentAsync(long personaje_id = 0, string? nombre = null, IUser? usuario = null)
    {
        await DeferAsync(ephemeral: false);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            BuildLookup(personaje_id, nombre, usuario));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Equipment(result.Data)
                : CharacterProfileViews.Result("📭 Equipo no encontrado", result.Message, false),
            ephemeral: false);
    }

    [SlashCommand("arbol", "Muestra el árbol de habilidades del personaje.")]
    public async Task TreeAsync(long personaje_id = 0, string? nombre = null, IUser? usuario = null)
    {
        await DeferAsync(ephemeral: false);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            BuildLookup(personaje_id, nombre, usuario));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.SkillTree(result.Data)
                : CharacterProfileViews.Result("📭 Árbol no encontrado", result.Message, false),
            ephemeral: false);
    }

    private async Task ShowCatalogAsync(
        string catalogType,
        string title,
        string intro,
        Func<Task<IReadOnlyList<CharacterCatalogOptionDto>>> loadOptions,
        bool publico)
    {
        await DeferAsync(ephemeral: !publico);

        IReadOnlyList<CharacterCatalogOptionDto> options = await loadOptions();

        if (options.Count == 0)
        {
            await FollowupAsync(embed: CharacterCatalogBrowserViews.Empty(catalogType), ephemeral: !publico);
            return;
        }

        await FollowupAsync(
            embed: CharacterCatalogBrowserViews.Detail(
                title,
                intro,
                options[0],
                0,
                options.Count),
            components: CharacterCatalogBrowserComponents.BrowserButtons(catalogType, 0, options.Count),
            ephemeral: !publico);
    }

    private static CharacterLookupDto BuildLookup(long personajeId, string? nombre, IUser? usuario)
    {
        return new CharacterLookupDto(
            personajeId > 0 ? (ulong)personajeId : null,
            string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim(),
            usuario?.Id);
    }
}
