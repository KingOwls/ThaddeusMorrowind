using Discord;
using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Modals;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Creation;
using ThaddeusMorrowind.Bot.Features.Characters.Experience;

namespace ThaddeusMorrowind.Bot.Discord.SlashCommands;

[Group("personaje", "Gestiona tus personajes.")]
public sealed class CharacterInteractiveSlashCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICharacterService _characterService;
    private readonly ICharacterCreationSessionStore _sessionStore;
    private readonly ICharacterExperienceService _characterExperienceService;

    public CharacterInteractiveSlashCommands(
        ICharacterService characterService,
        ICharacterCreationSessionStore sessionStore,
        ICharacterExperienceService characterExperienceService)
    {
        _characterService = characterService;
        _sessionStore = sessionStore;
        _characterExperienceService = characterExperienceService;
    }

    [SlashCommand("crear", "Inicia el panel guiado para crear un personaje.")]
    public async Task CreateAsync(
        [Summary(description: "Imagen opcional del personaje. Discord la convierte en URL.")] IAttachment? retrato = null)
    {
        _sessionStore.Start(new CharacterCreationSession
        {
            UserId = Context.User.Id,
            ChannelId = Context.Channel.Id,
            PortraitUrl = retrato?.Url
        });

        await RespondWithModalAsync<CreateCharacterModal>("character:create:modal");
    }

    [SlashCommand("lista", "Lista tus personajes.")]
    public async Task ListAsync()
    {
        IReadOnlyList<CharacterSummaryDto> characters =
            await _characterService.ListCharactersAsync(Context.User.Id);

        await RespondAsync(embed: CharacterViews.List(characters), ephemeral: true);
    }

    [SlashCommand("seleccionar", "Selecciona tu personaje activo.")]
    public async Task SelectAsync(long id)
    {
        if (id <= 0)
        {
            await RespondAsync(embed: CharacterViews.Error("El ID debe ser mayor que cero."), ephemeral: true);
            return;
        }

        CharacterDetailDto? character =
            await _characterService.SelectCharacterAsync(Context.User.Id, (ulong)id);

        if (character is null)
        {
            await RespondAsync(embed: CharacterViews.Error("No encontré ese personaje entre tus personajes activos."), ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: CharacterViews.Selected(character),
            components: CharacterCreationComponents.CharacterActions(character.Id),
            ephemeral: true);
    }

    [SlashCommand("ver", "Muestra la ficha de un personaje. Si no das ID, muestra el activo.")]
    public async Task ViewAsync(long id = 0)
    {
        ulong? characterId = id > 0 ? (ulong)id : null;

        CharacterDetailDto? character =
            await _characterService.GetCharacterAsync(Context.User.Id, characterId);

        if (character is null)
        {
            await RespondAsync(embed: CharacterViews.Error("No tienes personaje activo o el ID no existe."), ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: CharacterViews.DetailEmbed(character),
            components: CharacterCreationComponents.CharacterActions(character.Id),
            ephemeral: true);
    }

    [SlashCommand("editar", "Edita nombre, apodo o retrato de uno de tus personajes.")]
    public async Task EditAsync(
        [Summary(description: "ID del personaje. Usa /personaje lista para verlo.")] long id,
        [Summary(description: "Nuevo nombre. Deja vacío para conservar.")] string? nuevo_nombre = null,
        [Summary(description: "Nuevo apodo. Deja vacío para conservar.")] string? nuevo_apodo = null,
        [Summary(description: "Quitar retrato actual.")] bool quitar_retrato = false,
        [Summary(description: "Nuevo retrato opcional.")] IAttachment? retrato = null)
    {
        if (id <= 0)
        {
            await RespondAsync(embed: CharacterViews.Error("El ID debe ser mayor que cero."), ephemeral: true);
            return;
        }

        CharacterCreateResult result = await _characterService.UpdateCharacterAsync(
            Context.User.Id,
            (ulong)id,
            nuevo_nombre,
            nuevo_apodo,
            retrato?.Url,
            quitar_retrato);

        if (!result.Success || result.Character is null)
        {
            await RespondAsync(embed: CharacterViews.Error(result.Message), ephemeral: true);
            return;
        }

        await RespondAsync(
            text: "✅ Personaje actualizado.",
            embed: CharacterViews.DetailEmbed(result.Character),
            components: CharacterCreationComponents.CharacterActions(result.Character.Id),
            ephemeral: true);
    }

    [SlashCommand("eliminar", "Elimina un personaje. Solo administradores.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task DeleteAsync(
        [Summary(description: "ID del personaje a eliminar.")] long id,
        [Summary(description: "Debes marcar true para confirmar.")] bool confirmar = false,
        [Summary(description: "Razón opcional.")] string? razon = null)
    {
        if (!confirmar)
        {
            await RespondAsync(
                embed: CharacterViews.Error("Para eliminar un personaje debes usar `confirmar:true`. Esta acción hará borrado lógico."),
                ephemeral: true);
            return;
        }

        if (id <= 0)
        {
            await RespondAsync(embed: CharacterViews.Error("El ID debe ser mayor que cero."), ephemeral: true);
            return;
        }

        CharacterDeleteResult result = await _characterService.AdminDeleteCharacterAsync(
            (ulong)id,
            Context.User.Id,
            razon);

        if (!result.Success)
        {
            await RespondAsync(embed: CharacterViews.Error(result.Message), ephemeral: true);
            return;
        }

        Embed embed = new EmbedBuilder()
            .WithTitle("🗑️ Personaje eliminado")
            .WithDescription($"Se eliminó lógicamente el personaje **{result.DeletedCharacterName}** (`ID {result.DeletedCharacterId}`).")
            .AddField("Administrador", Context.User.Mention, inline: true)
            .AddField("Razón", string.IsNullOrWhiteSpace(razon) ? "No especificada." : razon, inline: false)
            .WithColor(Color.DarkRed)
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
    [SlashCommand("xp_agregar", "Agrega experiencia a un personaje. Solo administradores.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task AddExperienceAsync(
        [Summary(description: "ID del personaje.")] long id,
        [Summary(description: "Cantidad de XP a agregar.")] long cantidad,
        [Summary(description: "Razón opcional.")] string? razon = null)
    {
        if (id <= 0)
        {
            await RespondAsync(embed: CharacterViews.Error("El ID debe ser mayor que cero."), ephemeral: true);
            return;
        }

        if (cantidad <= 0)
        {
            await RespondAsync(embed: CharacterViews.Error("La cantidad debe ser mayor que cero."), ephemeral: true);
            return;
        }

        CharacterExperienceResult result = await _characterExperienceService.AdjustExperienceAsync(
            (ulong)id,
            cantidad,
            Context.User.Id,
            "admin_slash_add_xp",
            razon);

        await RespondAsync(embed: CharacterExperienceViews.Result(result), ephemeral: true);
    }

    [SlashCommand("xp_quitar", "Quita experiencia a un personaje. Solo administradores.")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task RemoveExperienceAsync(
        [Summary(description: "ID del personaje.")] long id,
        [Summary(description: "Cantidad de XP a quitar.")] long cantidad,
        [Summary(description: "Razón opcional.")] string? razon = null)
    {
        if (id <= 0)
        {
            await RespondAsync(embed: CharacterViews.Error("El ID debe ser mayor que cero."), ephemeral: true);
            return;
        }

        if (cantidad <= 0)
        {
            await RespondAsync(embed: CharacterViews.Error("La cantidad debe ser mayor que cero."), ephemeral: true);
            return;
        }

        CharacterExperienceResult result = await _characterExperienceService.AdjustExperienceAsync(
            (ulong)id,
            -cantidad,
            Context.User.Id,
            "admin_slash_remove_xp",
            razon);

        await RespondAsync(embed: CharacterExperienceViews.Result(result), ephemeral: true);
    }

}
