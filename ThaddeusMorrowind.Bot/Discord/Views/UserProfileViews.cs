using Discord;
using System.Text;
using ThaddeusMorrowind.Bot.Features.Users;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class UserProfileViews
{
    public static Embed Registered(UserProfileDto profile)
    {
        return BaseProfile(profile)
            .WithTitle("✅ Registro completado")
            .WithDescription($"El archivo de aventurero ha reconocido a **{DisplayName(profile)}**.\nTu perfil está listo para crear personajes, entrar a dungeons y acumular gloria cuestionablemente legal.")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed Profile(UserProfileDto profile)
    {
        return BaseProfile(profile)
            .WithTitle("📜 Perfil de Aventurero")
            .WithColor(Color.Gold)
            .Build();
    }

    public static Embed Wallet(UserWalletDto wallet)
    {
        return new EmbedBuilder()
            .WithTitle("💰 Billetera")
            .WithDescription("Recursos principales del usuario.")
            .WithColor(Color.Gold)
            .AddField("Oro", $"🪙 {wallet.Gold}", true)
            .AddField("Moneda premium", $"💎 {wallet.PremiumCurrency}", true)
            .AddField("Última actualización", FormatDate(wallet.UpdatedAt), false)
            .WithFooter("Thaddeus Morrowind · Economía")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed NotRegistered()
    {
        return new EmbedBuilder()
            .WithTitle("⚠️ No tienes perfil todavía")
            .WithDescription("Usa `/registro` o `!registro` para abrir tu archivo de aventurero.")
            .WithColor(Color.Orange)
            .WithFooter("Thaddeus Morrowind · Registro")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed Help()
    {
        StringBuilder text = new();
        text.AppendLine("**Comandos disponibles:**");
        text.AppendLine("`/registro` o `!registro`");
        text.AppendLine("`/perfil` o `!perfil`");
        text.AppendLine("`/perfil_actualizar` o `!perfil actualizar`");
        text.AppendLine("`/wallet` o `!wallet`");
        text.AppendLine();
        text.AppendLine("Los botones de personajes quedan preparados para la siguiente fase del MVP.");

        return new EmbedBuilder()
            .WithTitle("❔ Ayuda de usuario")
            .WithDescription(text.ToString())
            .WithColor(Color.Blue)
            .WithFooter("Thaddeus Morrowind · Ayuda")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed Info(string title, string message)
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .WithColor(Color.Blue)
            .WithFooter("Thaddeus Morrowind · Sistema")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed Error(string message)
    {
        return new EmbedBuilder()
            .WithTitle("⚠️ No se pudo completar la acción")
            .WithDescription(message)
            .WithColor(Color.Red)
            .WithFooter("Thaddeus Morrowind · Sistema")
            .WithCurrentTimestamp()
            .Build();
    }

    private static EmbedBuilder BaseProfile(UserProfileDto profile)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .AddField("Usuario", $"**{DisplayName(profile)}**", true)
            .AddField("Estado", FormatAccountStatus(profile.AccountStatus), true)
            .AddField("Oro", $"🪙 {profile.Gold}", true)
            .AddField("Moneda premium", $"💎 {profile.PremiumCurrency}", true)
            .AddField("Personajes registrados", $"🧙 {profile.CharacterCount}/{profile.MaxCharacterSlots}", true)
            .AddField("Personaje activo", profile.ActiveCharacterName ?? "Ninguno", true)
            .AddField("Registrado", FormatDate(profile.RegisteredAt), true)
            .AddField("Última interacción", profile.LastInteractionAt is null ? "Sin registro" : FormatDate(profile.LastInteractionAt.Value), true)
            .WithFooter("Thaddeus Morrowind · Perfil")
            .WithCurrentTimestamp();

        if (!string.IsNullOrWhiteSpace(profile.AvatarUrl))
        {
            builder.WithThumbnailUrl(profile.AvatarUrl);
        }

        return builder;
    }

    private static string DisplayName(UserProfileDto profile)
    {
        return string.IsNullOrWhiteSpace(profile.PublicNickname) ? profile.Username : profile.PublicNickname!;
    }

    private static string FormatAccountStatus(string status)
    {
        return status switch
        {
            "active" => "Activa",
            "disabled" => "Deshabilitada",
            "banned" => "Baneada",
            _ => status
        };
    }

    private static string FormatDate(DateTime date) => $"{date:yyyy-MM-dd HH:mm} UTC";
}
