using Discord.Commands;
using Discord.WebSocket;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Users;

namespace ThaddeusMorrowind.Bot.Discord.PrefixCommands;

public sealed class UserPrefixCommands : ModuleBase<SocketCommandContext>
{
    private readonly IUserService _userService;
    private readonly IUserWalletService _walletService;

    public UserPrefixCommands(IUserService userService, IUserWalletService walletService)
    {
        _userService = userService;
        _walletService = walletService;
    }

    [Command("registro")]
    [Alias("registrar", "register")]
    public async Task RegisterAsync()
    {
        UserProfileDto profile = await _userService.RegisterOrUpdateAsync(BuildDiscordUserContext(Context.User));
        await ReplyAsync(embed: UserProfileViews.Registered(profile), components: UserProfileComponents.ProfileButtons());
    }

    [Command("perfil")]
    [Alias("profile")]
    public async Task ProfileAsync(SocketUser? usuario = null)
    {
        SocketUser targetUser = usuario ?? Context.User;
        bool isOwnProfile = targetUser.Id == Context.User.Id;

        UserProfileDto? profile = await _userService.GetProfileAsync(targetUser.Id);

        if (profile is null)
        {
            if (isOwnProfile)
            {
                await ReplyAsync(
                    embed: UserProfileViews.NotRegistered(),
                    components: UserProfileComponents.RegisterButton());
            }
            else
            {
                await ReplyAsync(
                    embed: UserProfileViews.Info(
                        "📭 Perfil no encontrado",
                        $"**{targetUser.Username}** todavía no tiene perfil registrado."));
            }

            return;
        }

        if (isOwnProfile)
        {
            await _userService.TouchLastInteractionAsync(Context.User.Id, "profile_viewed_prefix_public");
        }

        await ReplyAsync(
            embed: UserProfileViews.Profile(profile),
            components: isOwnProfile ? UserProfileComponents.ProfileButtons() : null);
    }

    [Command("perfil actualizar")]
    [Alias("profile refresh")]
    public async Task RefreshProfileAsync()
    {
        UserProfileDto? profile = await _userService.UpdateDiscordIdentityAsync(BuildDiscordUserContext(Context.User));
        if (profile is null)
        {
            await ReplyAsync(embed: UserProfileViews.NotRegistered(), components: UserProfileComponents.RegisterButton());
            return;
        }
        await ReplyAsync(embed: UserProfileViews.Profile(profile), components: UserProfileComponents.ProfileButtons());
    }

    [Command("wallet")]
    [Alias("billetera", "monedas")]
    public async Task WalletAsync()
    {
        UserWalletDto? wallet = await _walletService.GetWalletAsync(Context.User.Id);
        if (wallet is null)
        {
            await ReplyAsync(embed: UserProfileViews.NotRegistered(), components: UserProfileComponents.RegisterButton());
            return;
        }
        await _userService.TouchLastInteractionAsync(Context.User.Id, "wallet_viewed_prefix");
        await ReplyAsync(embed: UserProfileViews.Wallet(wallet));
    }

    private static DiscordUserContextDto BuildDiscordUserContext(SocketUser user)
    {
        return new DiscordUserContextDto(
            user.Id,
            user.Username,
            user.Username,
            user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
    }
}
