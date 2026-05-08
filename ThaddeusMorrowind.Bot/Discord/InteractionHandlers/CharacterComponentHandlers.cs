using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.InteractionHandlers;

public sealed class CharacterComponentHandlers : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ICharacterService _characterService;

    public CharacterComponentHandlers(ICharacterService characterService)
    {
        _characterService = characterService;
    }

    [ComponentInteraction("character:select:*")]
    public async Task SelectButtonAsync(string characterId)
    {
        await DeferAsync(ephemeral: true);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.SelectAsync(
            Context.User.Id,
            new CharacterLookupDto(ParseId(characterId), null, null));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data, "⭐ Personaje activo")
                : CharacterProfileViews.Result("⚠️ No se pudo seleccionar", result.Message, false),
            components: result.Success && result.Data is not null ? CharacterProfileComponents.ForOwner(result.Data) : null,
            ephemeral: true);
    }

    [ComponentInteraction("character:stats:*")]
    public async Task StatsButtonAsync(string characterId)
    {
        await DeferAsync(ephemeral: false);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            new CharacterLookupDto(ParseId(characterId), null, null));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Stats(result.Data)
                : CharacterProfileViews.Result("📭 Stats no encontrados", result.Message, false),
            ephemeral: false);
    }

    [ComponentInteraction("character:tree:*")]
    public async Task TreeButtonAsync(string characterId)
    {
        await DeferAsync(ephemeral: false);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            new CharacterLookupDto(ParseId(characterId), null, null));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.SkillTree(result.Data)
                : CharacterProfileViews.Result("📭 Árbol no encontrado", result.Message, false),
            ephemeral: false);
    }

    [ComponentInteraction("character:equipment:*")]
    public async Task EquipmentButtonAsync(string characterId)
    {
        await DeferAsync(ephemeral: false);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.GetAsync(
            Context.User.Id,
            new CharacterLookupDto(ParseId(characterId), null, null));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Equipment(result.Data)
                : CharacterProfileViews.Result("📭 Equipo no encontrado", result.Message, false),
            ephemeral: false);
    }

    [ComponentInteraction("character:archive:*")]
    public async Task ArchiveButtonAsync(string characterId)
    {
        await DeferAsync(ephemeral: true);

        CharacterCommandResult<CharacterProfileDto> result = await _characterService.ArchiveAsync(
            Context.User.Id,
            new CharacterLookupDto(ParseId(characterId), null, null));

        await FollowupAsync(
            embed: result.Success && result.Data is not null
                ? CharacterProfileViews.Profile(result.Data, "📦 Personaje archivado")
                : CharacterProfileViews.Result("⚠️ No se pudo archivar", result.Message, false),
            ephemeral: true);
    }

    private static ulong? ParseId(string value)
    {
        return ulong.TryParse(value, out ulong id)
            ? id
            : null;
    }
}
