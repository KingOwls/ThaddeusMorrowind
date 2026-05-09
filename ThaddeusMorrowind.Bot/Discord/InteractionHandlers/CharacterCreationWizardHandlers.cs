using Discord;
using Discord.Interactions;
using Discord.WebSocket;
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
        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync(
                embed: CharacterProfileViews.Result(
                    "⚠️ Interacción inválida",
                    "No pude identificar el panel de creación.",
                    false),
                ephemeral: true);

            return;
        }

        CharacterCreationSessionDto? session = _sessionStore.Get(sessionId, Context.User.Id);

        if (session is null)
        {
            await component.UpdateAsync(message =>
            {
                message.Embed = CharacterCreationWizardViews.Expired();
                message.Components = new ComponentBuilder().Build();
            });

            return;
        }

        IReadOnlyList<CharacterCatalogOptionDto> nations = await _catalogService.GetNationsAsync();
        IReadOnlyList<CharacterCatalogOptionDto> roles = await _catalogService.GetRolesAsync();
        IReadOnlyList<CharacterCatalogOptionDto> professions = await _catalogService.GetProfessionsAsync();

        if (nations.Count == 0 || roles.Count == 0 || professions.Count == 0)
        {
            await component.UpdateAsync(message =>
            {
                message.Embed = CharacterCreationWizardViews.CatalogMissing();
                message.Components = new ComponentBuilder().Build();
            });

            return;
        }

        if (action == "cancel")
        {
            _sessionStore.Remove(sessionId, Context.User.Id);

            await component.UpdateAsync(message =>
            {
                message.Embed = CharacterCreationWizardViews.Cancelled();
                message.Components = new ComponentBuilder().Build();
            });

            return;
        }

        CharacterCreationSessionDto updated = session;

        if (action == "prev" || action == "next")
        {
            updated = MoveSelection(session, action, nations.Count, roles.Count, professions.Count);
            _sessionStore.Save(updated);

            await UpdateWizardPanelAsync(component, updated, nations, roles, professions);
            return;
        }

        if (action == "accept")
        {
            if (session.CurrentStep < 2)
            {
                CharacterCreationSessionDto? nextStep = _sessionStore.UpdateStep(
                    sessionId,
                    Context.User.Id,
                    session.CurrentStep + 1);

                if (nextStep is null)
                {
                    await component.UpdateAsync(message =>
                    {
                        message.Embed = CharacterCreationWizardViews.Expired();
                        message.Components = new ComponentBuilder().Build();
                    });

                    return;
                }

                await UpdateWizardPanelAsync(component, nextStep, nations, roles, professions);
                return;
            }

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

            await component.UpdateAsync(message =>
            {
                message.Embed = result.Success && result.Data is not null
                    ? CharacterProfileViews.Created(result.Data)
                    : CharacterProfileViews.Result("⚠️ No se pudo crear", result.Message, false);

                message.Components = result.Success && result.Data is not null
                    ? CharacterProfileComponents.ForOwner(result.Data)
                    : new ComponentBuilder().Build();
            });

            return;
        }

        await UpdateWizardPanelAsync(component, updated, nations, roles, professions);
    }

    private static async Task UpdateWizardPanelAsync(
        SocketMessageComponent component,
        CharacterCreationSessionDto session,
        IReadOnlyList<CharacterCatalogOptionDto> nations,
        IReadOnlyList<CharacterCatalogOptionDto> roles,
        IReadOnlyList<CharacterCatalogOptionDto> professions)
    {
        await component.UpdateAsync(message =>
        {
            message.Embed = CharacterCreationWizardViews.Wizard(
                session,
                ClampPick(nations, session.NationIndex),
                ClampPick(roles, session.RoleIndex),
                ClampPick(professions, session.ProfessionIndex),
                nations.Count,
                roles.Count,
                professions.Count);

            message.Components = CharacterCreationWizardComponents.WizardButtons(session);
        });
    }

    private static CharacterCreationSessionDto MoveSelection(
        CharacterCreationSessionDto session,
        string action,
        int nationTotal,
        int roleTotal,
        int professionTotal)
    {
        int delta = action == "prev" ? -1 : 1;

        return session.CurrentStep switch
        {
            0 => session with { NationIndex = Wrap(session.NationIndex + delta, nationTotal) },
            1 => session with { RoleIndex = Wrap(session.RoleIndex + delta, roleTotal) },
            2 => session with { ProfessionIndex = Wrap(session.ProfessionIndex + delta, professionTotal) },
            _ => session
        };
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
