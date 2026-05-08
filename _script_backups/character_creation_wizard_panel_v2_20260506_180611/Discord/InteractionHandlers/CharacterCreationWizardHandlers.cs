using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.InteractionHandlers;

public sealed class CharacterCreationWizardHandlers : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICharacterCreationSessionStore _sessionStore;
    private readonly ICharacterCatalogService _catalogService;
    private readonly ICharacterService _characterService;

    public CharacterCreationWizardHandlers(
        ICharacterCreationSessionStore sessionStore,
        ICharacterCatalogService catalogService,
        ICharacterService characterService)
    {
        _sessionStore = sessionStore;
        _catalogService = catalogService;
        _characterService = characterService;
    }

    [ComponentInteraction("character:create:*:*")]
    public async Task HandleCreationButtonAsync(string action, string sessionId)
    {
        await DeferAsync(ephemeral: true);

        CharacterCreationSessionDto? session = _sessionStore.Get(sessionId, Context.User.Id);

        if (session is null)
        {
            await FollowupAsync(embed: CharacterCreationWizardViews.Expired(), ephemeral: true);
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

        if (action == "cancel")
        {
            _sessionStore.Remove(sessionId, Context.User.Id);
            await FollowupAsync(embed: CharacterCreationWizardViews.Cancelled(), ephemeral: true);
            return;
        }

        if (action == "confirm")
        {
            CharacterCatalogOptionDto nation = ClampPick(nations, session.NationIndex);
            CharacterCatalogOptionDto role = ClampPick(roles, session.RoleIndex);
            CharacterCatalogOptionDto profession = ClampPick(professions, session.ProfessionIndex);

            CharacterCommandResult<CharacterProfileDto> result = await _characterService.CreateAsync(
                Context.User.Id,
                new CharacterCreateRequestDto(
                    session.Name,
                    session.Nickname,
                    session.ImageUrl,
                    nation.Key,
                    role.Key,
                    profession.Key));

            _sessionStore.Remove(sessionId, Context.User.Id);

            await FollowupAsync(
                embed: result.Success && result.Data is not null
                    ? CharacterProfileViews.Created(result.Data)
                    : CharacterProfileViews.Result("⚠️ No se pudo crear", result.Message, false),
                components: result.Success && result.Data is not null ? CharacterProfileComponents.ForOwner(result.Data) : null,
                ephemeral: true);

            return;
        }

        int nextNationIndex = session.NationIndex;
        int nextRoleIndex = session.RoleIndex;
        int nextProfessionIndex = session.ProfessionIndex;

        switch (action)
        {
            case "nation_prev":
                nextNationIndex = Wrap(session.NationIndex - 1, nations.Count);
                break;
            case "nation_next":
                nextNationIndex = Wrap(session.NationIndex + 1, nations.Count);
                break;
            case "role_prev":
                nextRoleIndex = Wrap(session.RoleIndex - 1, roles.Count);
                break;
            case "role_next":
                nextRoleIndex = Wrap(session.RoleIndex + 1, roles.Count);
                break;
            case "profession_prev":
                nextProfessionIndex = Wrap(session.ProfessionIndex - 1, professions.Count);
                break;
            case "profession_next":
                nextProfessionIndex = Wrap(session.ProfessionIndex + 1, professions.Count);
                break;
        }

        CharacterCreationSessionDto? updated = _sessionStore.UpdateIndexes(
            sessionId,
            Context.User.Id,
            nextNationIndex,
            nextRoleIndex,
            nextProfessionIndex);

        if (updated is null)
        {
            await FollowupAsync(embed: CharacterCreationWizardViews.Expired(), ephemeral: true);
            return;
        }

        await FollowupAsync(
            embed: CharacterCreationWizardViews.Wizard(
                updated,
                ClampPick(nations, updated.NationIndex),
                ClampPick(roles, updated.RoleIndex),
                ClampPick(professions, updated.ProfessionIndex),
                nations.Count,
                roles.Count,
                professions.Count),
            components: CharacterCreationWizardComponents.WizardButtons(updated),
            ephemeral: true);
    }

    private static int Wrap(int value, int total)
    {
        if (total <= 0)
        {
            return 0;
        }

        if (value < 0)
        {
            return total - 1;
        }

        if (value >= total)
        {
            return 0;
        }

        return value;
    }

    private static CharacterCatalogOptionDto ClampPick(
        IReadOnlyList<CharacterCatalogOptionDto> options,
        int index)
    {
        if (index < 0)
        {
            return options[0];
        }

        if (index >= options.Count)
        {
            return options[^1];
        }

        return options[index];
    }
}
