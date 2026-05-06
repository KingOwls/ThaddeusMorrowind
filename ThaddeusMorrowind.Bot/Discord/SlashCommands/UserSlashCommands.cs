using Discord;
using Discord.Interactions;
using ThaddeusMorrowind.Bot.Discord.Components;
using ThaddeusMorrowind.Bot.Discord.Views;
using ThaddeusMorrowind.Bot.Features.Users;

namespace ThaddeusMorrowind.Bot.Discord.SlashCommands;

public sealed class UserSlashCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IUserService _userService;
    private readonly IUserWalletService _walletService;

    public UserSlashCommands(IUserService userService, IUserWalletService walletService)
    {
        _userService = userService;
        _walletService = walletService;
    }

    [SlashCommand("registro", "Crea o actualiza tu perfil de aventurero.")]
    public async Task RegisterAsync()
    {
        await DeferAsync(ephemeral: true);
        UserProfileDto profile = await _userService.RegisterOrUpdateAsync(BuildDiscordUserContext(Context.User));
        await FollowupAsync(embed: UserProfileViews.Registered(profile), components: UserProfileComponents.ProfileButtons(), ephemeral: true);
    }

    [SlashCommand("perfil", "Muestra un perfil de aventurero. Si no eliges usuario, muestra el tuyo.")]
    public async Task ProfileAsync(IUser? usuario = null)
    {
        // Público: este perfil es visible para el canal.
        await DeferAsync(ephemeral: false);

        IUser targetUser = usuario ?? Context.User;
        bool isOwnProfile = targetUser.Id == Context.User.Id;

        UserProfileDto? profile = await _userService.GetProfileAsync(targetUser.Id);

        if (profile is null)
        {
            if (isOwnProfile)
            {
                await FollowupAsync(
                    embed: UserProfileViews.NotRegistered(),
                    components: UserProfileComponents.RegisterButton(),
                    ephemeral: false);
            }
            else
            {
                await FollowupAsync(
                    embed: UserProfileViews.Info(
                        "📭 Perfil no encontrado",
                        $"**{targetUser.Username}** todavía no tiene perfil registrado."),
                    ephemeral: false);
            }

            return;
        }

        if (isOwnProfile)
        {
            await _userService.TouchLastInteractionAsync(Context.User.Id, "profile_viewed_public");
        }

        await FollowupAsync(
            embed: UserProfileViews.Profile(profile),
            components: isOwnProfile ? UserProfileComponents.ProfileButtons() : null,
            ephemeral: false);
    }

    [SlashCommand("perfil_actualizar", "Sincroniza tu nombre e imagen de Discord con el bot.")]
    public async Task RefreshProfileAsync()
    {
        await DeferAsync(ephemeral: true);
        UserProfileDto? profile = await _userService.UpdateDiscordIdentityAsync(BuildDiscordUserContext(Context.User));
        if (profile is null)
        {
            await FollowupAsync(embed: UserProfileViews.NotRegistered(), components: UserProfileComponents.RegisterButton(), ephemeral: true);
            return;
        }
        await FollowupAsync(embed: UserProfileViews.Profile(profile), components: UserProfileComponents.ProfileButtons(), ephemeral: true);
    }

    [SlashCommand("wallet", "Muestra tu billetera.")]
    public async Task WalletAsync()
    {
        await DeferAsync(ephemeral: true);
        UserWalletDto? wallet = await _walletService.GetWalletAsync(Context.User.Id);
        if (wallet is null)
        {
            await FollowupAsync(embed: UserProfileViews.NotRegistered(), components: UserProfileComponents.RegisterButton(), ephemeral: true);
            return;
        }
        await _userService.TouchLastInteractionAsync(Context.User.Id, "wallet_viewed");
        await FollowupAsync(embed: UserProfileViews.Wallet(wallet), ephemeral: true);
    }

    private static DiscordUserContextDto BuildDiscordUserContext(IUser user)
    {
        return new DiscordUserContextDto(
            user.Id,
            user.Username,
            user.Username,
            user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
    }
}
