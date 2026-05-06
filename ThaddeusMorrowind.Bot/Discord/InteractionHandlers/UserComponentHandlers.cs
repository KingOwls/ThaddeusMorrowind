using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Users;

namespace ThaddeusMorrowind.Bot.Discord.InteractionHandlers;

public sealed class UserComponentHandlers : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IUserService _userService;

    public UserComponentHandlers(IUserService userService)
    {
        _userService = userService;
    }

    [ComponentInteraction("user:register")]
    public async Task RegisterButtonAsync()
    {
        await DeferAsync(ephemeral: true);
        UserProfileDto profile = await _userService.RegisterOrUpdateAsync(BuildDiscordUserContext());
        await FollowupAsync(embed: UserProfileViews.Registered(profile), components: UserProfileComponents.ProfileButtons(), ephemeral: true);
    }

    [ComponentInteraction("user:profile:refresh")]
    public async Task RefreshButtonAsync()
    {
        await DeferAsync(ephemeral: true);
        UserProfileDto? profile = await _userService.UpdateDiscordIdentityAsync(BuildDiscordUserContext());
        if (profile is null)
        {
            await FollowupAsync(embed: UserProfileViews.NotRegistered(), components: UserProfileComponents.RegisterButton(), ephemeral: true);
            return;
        }
        await FollowupAsync(embed: UserProfileViews.Profile(profile), components: UserProfileComponents.ProfileButtons(), ephemeral: true);
    }

    [ComponentInteraction("user:characters:list")]
    public async Task CharacterListButtonAsync()
    {
        await RespondAsync(embed: UserProfileViews.Info("📋 Personajes", "El listado de personajes queda reservado para la siguiente fase del MVP."), ephemeral: true);
    }

    [ComponentInteraction("user:characters:create")]
    public async Task CharacterCreateButtonAsync()
    {
        await RespondAsync(embed: UserProfileViews.Info("🧙 Crear personaje", "La creación de personajes queda preparada para la siguiente fase del MVP."), ephemeral: true);
    }

    [ComponentInteraction("user:help")]
    public async Task HelpButtonAsync()
    {
        await RespondAsync(embed: UserProfileViews.Help(), ephemeral: true);
    }

    private DiscordUserContextDto BuildDiscordUserContext()
    {
        return new DiscordUserContextDto(
            Context.User.Id,
            Context.User.Username,
            Context.User.Username,
            Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());
    }
}
