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

    public CharacterSlashCommands(
        ICharacterService characterService,
        ICharacterCatalogService catalogService)
    {
        _characterService = characterService;
        _catalogService = catalogService;
    }


    [SlashCommand("naciones", "Muestra las naciones disponibles para crear personajes.")]
    public async Task NationsAsync(bool publico = false)
    {
        await DeferAsync(ephemeral: !publico);

        IReadOnlyList<CharacterCatalogOptionDto> options = await _catalogService.GetNationsAsync();

        if (options.Count == 0)
        {
            await FollowupAsync(embed: CharacterCatalogViews.Missing("nation"), ephemeral: !publico);
            return;
        }

        await FollowupAsync(
            embed: CharacterCatalogViews.List(
                "🏳️ Naciones disponibles",
                "Las naciones definen identidad, estilo narrativo y parte del crecimiento del personaje.",
                options),
            components: CharacterCatalogComponents.CatalogMenu("nation", options),
            ephemeral: !publico);
    }

    [SlashCommand("roles", "Muestra los roles disponibles para crear personajes.")]
    public async Task RolesAsync(bool publico = false)
    {
        await DeferAsync(ephemeral: !publico);

        IReadOnlyList<CharacterCatalogOptionDto> options = await _catalogService.GetRolesAsync();

        if (options.Count == 0)
        {
            await FollowupAsync(embed: CharacterCatalogViews.Missing("role"), ephemeral: !publico);
            return;
        }

        await FollowupAsync(
            embed: CharacterCatalogViews.List(
                "⚔️ Roles disponibles",
                "Los roles definen el trabajo principal del personaje en combate y equipo.",
                options),
            components: CharacterCatalogComponents.CatalogMenu("role", options),
            ephemeral: !publico);
    }

    [SlashCommand("profesiones", "Muestra las profesiones disponibles para crear personajes.")]
    public async Task ProfessionsAsync(bool publico = false)
    {
        await DeferAsync(ephemeral: !publico);

        IReadOnlyList<CharacterCatalogOptionDto> options = await _catalogService.GetProfessionsAsync();

        if (options.Count == 0)
        {
            await FollowupAsync(embed: CharacterCatalogViews.Missing("profession"), ephemeral: !publico);
            return;
        }

        await FollowupAsync(
            embed: CharacterCatalogViews.List(
                "🧰 Profesiones disponibles",
                "Las profesiones definen utilidad, exploración, recursos y eventos especiales.",
                options),
            components: CharacterCatalogComponents.CatalogMenu("profession", options),
            ephemeral: !publico);
    }

    [SlashCommand("crear", "Crea un personaje nuevo.")]
    public async Task CreateAsync(
        string nombre,
        string nacion,
        string rol,
        string profesion,
        string? apodo = null,
        string? imagen_url = null)
    {
        await DeferAsync(ephemeral: true);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.CreateAsync(
            Context.User.Id,
            new CharacterCreateRequestDto(nombre, apodo, imagen_url, nacion, rol, profesion));

        if (!result.Success || result.Data is null)
        {
            await FollowupAsync(embed: CharacterProfileViews.Result("⚠️ No se pudo crear el personaje", result.Message, false), ephemeral: true);
            return;
        }

        await FollowupAsync(
            embed: CharacterProfileViews.Created(result.Data),
            components: CharacterProfileComponents.ForOwner(result.Data),
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

        CharacterLookupDto lookup = BuildLookup(personaje_id, nombre, usuario);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            lookup);

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
        string? nueva_imagen_url = null)
    {
        await DeferAsync(ephemeral: true);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.EditAsync(
            Context.User.Id,
            new CharacterEditRequestDto(
                personaje_id > 0 ? (ulong)personaje_id : null,
                nombre_actual,
                nuevo_nombre,
                nuevo_apodo,
                nueva_imagen_url));

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

    private static CharacterLookupDto BuildLookup(long personajeId, string? nombre, IUser? usuario)
    {
        return new CharacterLookupDto(
            personajeId > 0 ? (ulong)personajeId : null,
            string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim(),
            usuario?.Id);
    }
}
