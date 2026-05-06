using Discord;

namespace ThaddeusMorrowind.Bot.Discord.Components;

public static class UserProfileComponents
{
    public static MessageComponent ProfileButtons()
    {
        return new ComponentBuilder()
            .WithButton("Ver personajes", "user:characters:list", ButtonStyle.Primary, new Emoji("📋"))
            .WithButton("Crear personaje", "user:characters:create", ButtonStyle.Success, new Emoji("🧙"))
            .WithButton("Actualizar perfil", "user:profile:refresh", ButtonStyle.Secondary, new Emoji("🔄"))
            .WithButton("Ayuda", "user:help", ButtonStyle.Secondary, new Emoji("❔"))
            .Build();
    }

    public static MessageComponent RegisterButton()
    {
        return new ComponentBuilder()
            .WithButton("Registrarme", "user:register", ButtonStyle.Success, new Emoji("✅"))
            .Build();
    }
}
