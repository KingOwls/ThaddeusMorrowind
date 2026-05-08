using Discord.Interactions;
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

    [ComponentInteraction("character:catalog:*:select")]
    public async Task SelectCatalogOptionAsync(string catalogType, string[] selectedValues)
    {
        await DeferAsync(ephemeral: true);

        string? selectedKey = selectedValues.FirstOrDefault();

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
