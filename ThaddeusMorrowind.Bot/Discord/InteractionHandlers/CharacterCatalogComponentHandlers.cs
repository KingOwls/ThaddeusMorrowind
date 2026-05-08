using Discord.Interactions;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.InteractionHandlers;

public sealed class CharacterCatalogComponentHandlers : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICharacterCatalogService _catalogService;

    public CharacterCatalogComponentHandlers(ICharacterCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [ComponentInteraction("character:catalog:nation:select")]
    public Task SelectNationAsync()
    {
        return HandleSelectAsync("nation");
    }

    [ComponentInteraction("character:catalog:role:select")]
    public Task SelectRoleAsync()
    {
        return HandleSelectAsync("role");
    }

    [ComponentInteraction("character:catalog:profession:select")]
    public Task SelectProfessionAsync()
    {
        return HandleSelectAsync("profession");
    }

    private async Task HandleSelectAsync(string catalogType)
    {
        await DeferAsync(ephemeral: true);

        string? selectedKey = null;

        if (Context.Interaction is SocketMessageComponent component)
        {
            selectedKey = component.Data.Values.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(selectedKey))
        {
            await FollowupAsync(
                embed: CharacterCatalogViews.Missing(catalogType),
                ephemeral: true);

            return;
        }

        CharacterCatalogOptionDto? option = await _catalogService.GetByKeyAsync(
            catalogType,
            selectedKey);

        await FollowupAsync(
            embed: option is null
                ? CharacterCatalogViews.Missing(catalogType)
                : CharacterCatalogViews.Detail(option),
            ephemeral: true);
    }
}
