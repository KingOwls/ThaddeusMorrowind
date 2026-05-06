using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Characters;
using ThaddeusMorrowind.Bot.Features.Characters.Creation;

namespace ThaddeusMorrowind.Bot.Discord.PrefixCommands;

[Group("personaje")]
[Alias("pj")]
public sealed class CharacterPrefixCommands : ModuleBase<SocketCommandContext>
{
    private readonly ICharacterService _characterService;
    private readonly ICharacterCreationSessionStore _sessionStore;

    public CharacterPrefixCommands(
        ICharacterService characterService,
        ICharacterCreationSessionStore sessionStore)
    {
        _characterService = characterService;
        _sessionStore = sessionStore;
    }

    [Command("crear")]
    [Alias("create")]
    public async Task CreateAsync([Remainder] string? portraitUrl = null)
    {
        string? attachmentUrl = Context.Message.Attachments.FirstOrDefault()?.Url;
        string? finalPortraitUrl = attachmentUrl ?? NormalizeOptionalUrl(portraitUrl);

        _sessionStore.Start(new CharacterCreationSession
        {
            UserId = Context.User.Id,
            ChannelId = Context.Channel.Id,
            PortraitUrl = finalPortraitUrl
        });

        await ReplyAsync(
            embed: CharacterViews.CreationIntro(),
            components: CharacterCreationComponents.StartButton());
    }

    [Command("lista")]
    [Alias("list")]
    public async Task ListAsync()
    {
        IReadOnlyList<CharacterSummaryDto> characters =
            await _characterService.ListCharactersAsync(Context.User.Id);

        await ReplyAsync(embed: CharacterViews.List(characters));
    }

    [Command("seleccionar")]
    [Alias("select")]
    public async Task SelectAsync(ulong id)
    {
        CharacterDetailDto? character =
            await _characterService.SelectCharacterAsync(Context.User.Id, id);

        if (character is null)
        {
            await ReplyAsync(embed: CharacterViews.Error("No encontré ese personaje entre tus personajes activos."));
            return;
        }

        await ReplyAsync(
            embed: CharacterViews.Selected(character),
            components: CharacterCreationComponents.CharacterActions(character.Id));
    }

    [Command("ver")]
    [Alias("view")]
    public async Task ViewAsync(ulong id = 0)
    {
        ulong? characterId = id > 0 ? id : null;

        CharacterDetailDto? character =
            await _characterService.GetCharacterAsync(Context.User.Id, characterId);

        if (character is null)
        {
            await ReplyAsync(embed: CharacterViews.Error("No tienes personaje activo o el ID no existe."));
            return;
        }

        await ReplyAsync(
            embed: CharacterViews.DetailEmbed(character),
            components: CharacterCreationComponents.CharacterActions(character.Id));
    }

    [Command("editar")]
    [Alias("edit")]
    public async Task EditAsync(ulong id, [Remainder] string? payload = null)
    {
        string? attachmentUrl = Context.Message.Attachments.FirstOrDefault()?.Url;
        (string? newName, string? newNickname, string? portraitUrl, bool clearPortrait) = ParseEditPayload(payload);

        portraitUrl = attachmentUrl ?? portraitUrl;

        CharacterCreateResult result = await _characterService.UpdateCharacterAsync(
            Context.User.Id,
            id,
            newName,
            newNickname,
            portraitUrl,
            clearPortrait);

        if (!result.Success || result.Character is null)
        {
            await ReplyAsync(embed: CharacterViews.Error(result.Message));
            return;
        }

        await ReplyAsync(
            message: "✅ Personaje actualizado.",
            embed: CharacterViews.DetailEmbed(result.Character),
            components: CharacterCreationComponents.CharacterActions(result.Character.Id));
    }

    [Command("eliminar")]
    [Alias("delete", "borrar")]
    public async Task DeleteAsync(ulong id, [Remainder] string? reason = null)
    {
        if (!IsAdmin(Context))
        {
            await ReplyAsync(embed: CharacterViews.Error("Solo un administrador puede eliminar personajes."));
            return;
        }

        CharacterDeleteResult result = await _characterService.AdminDeleteCharacterAsync(
            id,
            Context.User.Id,
            reason);

        if (!result.Success)
        {
            await ReplyAsync(embed: CharacterViews.Error(result.Message));
            return;
        }

        Embed embed = new EmbedBuilder()
            .WithTitle("🗑️ Personaje eliminado")
            .WithDescription($"Se eliminó lógicamente el personaje **{result.DeletedCharacterName}** (`ID {result.DeletedCharacterId}`).")
            .AddField("Administrador", Context.User.Mention, inline: true)
            .AddField("Razón", string.IsNullOrWhiteSpace(reason) ? "No especificada." : reason, inline: false)
            .WithColor(Color.DarkRed)
            .WithCurrentTimestamp()
            .Build();

        await ReplyAsync(embed: embed);
    }

    private static bool IsAdmin(SocketCommandContext context)
    {
        return context.User is SocketGuildUser guildUser
            && guildUser.GuildPermissions.Administrator;
    }

    private static (string? newName, string? newNickname, string? portraitUrl, bool clearPortrait) ParseEditPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return (null, null, null, false);
        }

        string[] parts = payload.Split('|', StringSplitOptions.TrimEntries);

        string? newName = parts.Length > 0 ? NormalizeOptionalText(parts[0]) : null;
        string? newNickname = parts.Length > 1 ? NormalizeOptionalText(parts[1]) : null;
        string? portraitUrl = parts.Length > 2 ? NormalizeOptionalUrl(parts[2]) : null;

        bool clearPortrait = parts.Any(x =>
            x.Equals("quitar_retrato", StringComparison.OrdinalIgnoreCase) ||
            x.Equals("sin_retrato", StringComparison.OrdinalIgnoreCase));

        return (newName, newNickname, portraitUrl, clearPortrait);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (trimmed is "-" or "_" or ".")
        {
            return null;
        }

        return trimmed;
    }

    private static string? NormalizeOptionalUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (trimmed.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("ninguna", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("sin imagen", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
        {
            return trimmed;
        }

        return null;
    }
}
