using Discord.Interactions;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.InteractionHandlers;

public sealed class CharacterCatalogBrowserHandlers : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICharacterCatalogService _catalogService;

    public CharacterCatalogBrowserHandlers(ICharacterCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [ComponentInteraction("character:catalogbrowse:nation:*:*")]
    public Task BrowseNationAsync(string action, string indexText)
    {
        return HandleBrowseAsync("nation", action, indexText);
    }

    [ComponentInteraction("character:catalogbrowse:role:*:*")]
    public Task BrowseRoleAsync(string action, string indexText)
    {
        return HandleBrowseAsync("role", action, indexText);
    }

    [ComponentInteraction("character:catalogbrowse:profession:*:*")]
    public Task BrowseProfessionAsync(string action, string indexText)
    {
        return HandleBrowseAsync("profession", action, indexText);
    }

    private async Task HandleBrowseAsync(
        string catalogType,
        string action,
        string indexText)
    {
        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync(
                embed: CharacterCatalogBrowserViews.Empty(catalogType),
                ephemeral: true);

            return;
        }

        IReadOnlyList<CharacterCatalogOptionDto> options = await LoadOptionsAsync(catalogType);

        if (options.Count == 0)
        {
            await component.UpdateAsync(message =>
            {
                message.Embed = CharacterCatalogBrowserViews.Empty(catalogType);
                message.Components = new Discord.ComponentBuilder().Build();
            });

            return;
        }

        int currentIndex = int.TryParse(indexText, out int parsed)
            ? parsed
            : 0;

        int nextIndex = action == "prev"
            ? Wrap(currentIndex - 1, options.Count)
            : Wrap(currentIndex + 1, options.Count);

        CharacterCatalogOptionDto option = options[nextIndex];

        await component.UpdateAsync(message =>
        {
            message.Embed = CharacterCatalogBrowserViews.Detail(
                CatalogTitle(catalogType),
                CatalogIntro(catalogType),
                option,
                nextIndex,
                options.Count);

            message.Components = CharacterCatalogBrowserComponents.BrowserButtons(
                catalogType,
                nextIndex,
                options.Count);
        });
    }

    private Task<IReadOnlyList<CharacterCatalogOptionDto>> LoadOptionsAsync(string catalogType)
    {
        return catalogType switch
        {
            "nation" => _catalogService.GetNationsAsync(),
            "role" => _catalogService.GetRolesAsync(),
            "profession" => _catalogService.GetProfessionsAsync(),
            _ => Task.FromResult((IReadOnlyList<CharacterCatalogOptionDto>)Array.Empty<CharacterCatalogOptionDto>())
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

    private static string CatalogTitle(string catalogType)
    {
        return catalogType switch
        {
            "nation" => "🏳️ Naciones",
            "role" => "⚔️ Roles",
            "profession" => "🧰 Profesiones",
            _ => "📚 Catálogo"
        };
    }

    private static string CatalogIntro(string catalogType)
    {
        return catalogType switch
        {
            "nation" => "Las naciones definen identidad, afinidad narrativa y crecimiento base.",
            "role" => "Los roles definen función de combate y estilo principal.",
            "profession" => "Las profesiones definen utilidad, exploración y recursos.",
            _ => "Catálogo visual."
        };
    }
}
