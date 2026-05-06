using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Modals;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Creation;

namespace ThaddeusMorrowind.Bot.Discord.InteractionHandlers;

public sealed class CharacterCreationInteractionHandlers : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICharacterService _characterService;
    private readonly ICharacterCreationSessionStore _sessionStore;

    public CharacterCreationInteractionHandlers(
        ICharacterService characterService,
        ICharacterCreationSessionStore sessionStore)
    {
        _characterService = characterService;
        _sessionStore = sessionStore;
    }

    [ComponentInteraction(CharacterCreationComponents.StartButtonId)]
    public async Task StartButtonAsync()
    {
        await RespondWithModalAsync<CreateCharacterModal>("character:create:modal");
    }

    [ModalInteraction("character:create:modal")]
    public async Task CreateModalAsync(CreateCharacterModal modal)
    {
        string? portraitUrl = null;

        if (_sessionStore.TryGet(Context.User.Id, out CharacterCreationSession existingSession))
        {
            portraitUrl = existingSession.PortraitUrl;
        }

        string nickname = string.IsNullOrWhiteSpace(modal.Nickname) ? modal.Name : modal.Nickname;

        _sessionStore.Start(new CharacterCreationSession
        {
            UserId = Context.User.Id,
            ChannelId = Context.Channel.Id,
            Name = modal.Name.Trim(),
            Nickname = nickname.Trim(),
            PortraitUrl = portraitUrl
        });

        IReadOnlyList<CharacterCatalogOptionDto> nations = await _characterService.GetNationOptionsAsync();

        if (nations.Count == 0)
        {
            await RespondAsync(
                embed: CharacterViews.Error("No hay naciones activas para crear personajes. Revisa el catálogo `nations`."),
                ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: CharacterViews.ChooseNation(modal.Name.Trim(), nickname.Trim()),
            components: CharacterCreationComponents.NationSelect(nations),
            ephemeral: true);
    }

    [ComponentInteraction(CharacterCreationComponents.NationSelectId)]
    public async Task NationSelectedAsync(string[] selections)
    {
        if (!_sessionStore.TryGet(Context.User.Id, out CharacterCreationSession session))
        {
            await RespondAsync(embed: CharacterViews.Error("La sesión de creación expiró. Usa `/personaje crear` de nuevo."), ephemeral: true);
            return;
        }

        if (!ulong.TryParse(selections.FirstOrDefault(), out ulong nationId))
        {
            await RespondAsync(embed: CharacterViews.Error("Selección de nación inválida."), ephemeral: true);
            return;
        }

        session.NationId = nationId;
        session.Refresh();

        CharacterCatalogOptionDto? nation = await _characterService.GetNationOptionAsync(nationId);

        if (nation is null)
        {
            await RespondAsync(embed: CharacterViews.Error("La nación seleccionada ya no está disponible."), ephemeral: true);
            return;
        }

        IReadOnlyList<CharacterCatalogOptionDto> professions = await _characterService.GetProfessionOptionsAsync();

        if (professions.Count == 0)
        {
            await RespondAsync(
                embed: CharacterViews.Error("No hay profesiones activas para crear personajes. Revisa el catálogo `professions`."),
                ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: CharacterViews.ChooseProfession(nation),
            components: CharacterCreationComponents.ProfessionSelect(professions),
            ephemeral: true);
    }

    [ComponentInteraction(CharacterCreationComponents.ProfessionSelectId)]
    public async Task ProfessionSelectedAsync(string[] selections)
    {
        if (!_sessionStore.TryGet(Context.User.Id, out CharacterCreationSession session))
        {
            await RespondAsync(embed: CharacterViews.Error("La sesión de creación expiró. Usa `/personaje crear` de nuevo."), ephemeral: true);
            return;
        }

        if (!ulong.TryParse(selections.FirstOrDefault(), out ulong professionId))
        {
            await RespondAsync(embed: CharacterViews.Error("Selección de profesión inválida."), ephemeral: true);
            return;
        }

        session.ProfessionId = professionId;
        session.Refresh();

        CharacterCatalogOptionDto? profession = await _characterService.GetProfessionOptionAsync(professionId);

        if (profession is null)
        {
            await RespondAsync(embed: CharacterViews.Error("La profesión seleccionada ya no está disponible."), ephemeral: true);
            return;
        }

        IReadOnlyList<CharacterCatalogOptionDto> roles = await _characterService.GetRoleOptionsAsync();

        if (roles.Count == 0)
        {
            await RespondAsync(
                embed: CharacterViews.Error("No hay roles activos para crear personajes. Revisa el catálogo `roles`."),
                ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: CharacterViews.ChooseRole(profession),
            components: CharacterCreationComponents.RoleSelect(roles),
            ephemeral: true);
    }

    [ComponentInteraction(CharacterCreationComponents.RoleSelectId)]
    public async Task RoleSelectedAsync(string[] selections)
    {
        if (!_sessionStore.TryGet(Context.User.Id, out CharacterCreationSession session))
        {
            await RespondAsync(embed: CharacterViews.Error("La sesión de creación expiró. Usa `/personaje crear` de nuevo."), ephemeral: true);
            return;
        }

        if (session.NationId is null || session.ProfessionId is null)
        {
            await RespondAsync(embed: CharacterViews.Error("Faltan datos de nación o profesión. Reinicia con `/personaje crear`."), ephemeral: true);
            return;
        }

        if (!ulong.TryParse(selections.FirstOrDefault(), out ulong roleId))
        {
            await RespondAsync(embed: CharacterViews.Error("Selección de rol inválida."), ephemeral: true);
            return;
        }

        CharacterCatalogOptionDto? role = await _characterService.GetRoleOptionAsync(roleId);

        if (role is null)
        {
            await RespondAsync(embed: CharacterViews.Error("El rol seleccionado ya no está disponible."), ephemeral: true);
            return;
        }

        CharacterCreateResult result = await _characterService.CreateCharacterFromCatalogAsync(
            Context.User.Id,
            Context.User.Username,
            Context.User.GlobalName,
            session.Name,
            session.Nickname,
            session.NationId.Value,
            session.ProfessionId.Value,
            roleId,
            session.PortraitUrl);

        _sessionStore.Remove(Context.User.Id);

        if (!result.Success || result.Character is null)
        {
            await RespondAsync(embed: CharacterViews.Error(result.Message), ephemeral: true);
            return;
        }

        await RespondAsync(
            embed: CharacterViews.Created(result.Character),
            components: CharacterCreationComponents.CharacterActions(result.Character.Id),
            ephemeral: true);
    }

    [ComponentInteraction("character:select:*")]
    public async Task SelectButtonAsync(string idText)
    {
        if (!ulong.TryParse(idText, out ulong characterId))
        {
            await RespondAsync(embed: CharacterViews.Error("ID inválido."), ephemeral: true);
            return;
        }

        CharacterDetailDto? character = await _characterService.SelectCharacterAsync(Context.User.Id, characterId);

        if (character is null)
        {
            await RespondAsync(embed: CharacterViews.Error("No encontré ese personaje entre tus personajes activos."), ephemeral: true);
            return;
        }

        await RespondAsync(embed: CharacterViews.Selected(character), ephemeral: true);
    }

    [ComponentInteraction("character:stats:*")]
    public async Task StatsButtonAsync(string idText)
    {
        if (!ulong.TryParse(idText, out ulong characterId))
        {
            await RespondAsync(embed: CharacterViews.Error("ID inválido."), ephemeral: true);
            return;
        }

        CharacterDetailDto? character = await _characterService.GetCharacterAsync(Context.User.Id, characterId);

        if (character is null)
        {
            await RespondAsync(embed: CharacterViews.Error("No encontré ese personaje."), ephemeral: true);
            return;
        }

        await RespondAsync(embed: CharacterViews.FullStats(character), ephemeral: true);
    }

    [ComponentInteraction("character:artifacts:*")]
    public async Task ArtifactsButtonAsync(string _)
    {
        await RespondAsync(embed: CharacterViews.ComingSoon("🏺 Artefactos"), ephemeral: true);
    }

    [ComponentInteraction("character:skills:*")]
    public async Task SkillsButtonAsync(string _)
    {
        await RespondAsync(embed: CharacterViews.ComingSoon("✨ Habilidades"), ephemeral: true);
    }
}
