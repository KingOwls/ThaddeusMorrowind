using Discord;
using System.Text;
using ThaddeusMorrowind.Bot.Features.Characters.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class CharacterCreationWizardViews
{
    public static Embed Wizard(
        CharacterCreationSessionDto session,
        CharacterCatalogOptionDto nation,
        CharacterCatalogOptionDto role,
        CharacterCatalogOptionDto profession,
        int nationTotal,
        int roleTotal,
        int professionTotal)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle("🧙 Creación de personaje")
            .WithDescription(BuildDescription(session, nationTotal, roleTotal, professionTotal))
            .WithColor(Color.Purple)
            .AddField("Nación seleccionada", BuildOption(nation, session.NationIndex, nationTotal), inline: false)
            .AddField("Rol seleccionado", BuildOption(role, session.RoleIndex, roleTotal), inline: false)
            .AddField("Profesión seleccionada", BuildOption(profession, session.ProfessionIndex, professionTotal), inline: false)
            .WithFooter($"Sesión {session.SessionId} · Expira en 10 minutos")
            .WithCurrentTimestamp();

        string? thumbnailUrl = FirstNotEmpty(session.ImageUrl, role.IconUrl, nation.IconUrl, profession.IconUrl);
        string? bannerUrl = FirstNotEmpty(nation.BannerUrl, role.BannerUrl, profession.BannerUrl);

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            builder.WithThumbnailUrl(thumbnailUrl);
        }

        if (!string.IsNullOrWhiteSpace(bannerUrl))
        {
            builder.WithImageUrl(bannerUrl);
        }

        return builder.Build();
    }

    public static Embed Cancelled()
    {
        return new EmbedBuilder()
            .WithTitle("🧹 Creación cancelada")
            .WithDescription("La sesión de creación fue cancelada. No se guardó ningún personaje.")
            .WithColor(Color.Orange)
            .WithFooter("Thaddeus Morrowind · Personajes")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed Expired()
    {
        return new EmbedBuilder()
            .WithTitle("⌛ Sesión expirada")
            .WithDescription("La sesión de creación ya no existe o expiró. Usa `/personaje crear` otra vez.")
            .WithColor(Color.Orange)
            .WithFooter("Thaddeus Morrowind · Personajes")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed CatalogMissing()
    {
        return new EmbedBuilder()
            .WithTitle("⚠️ Catálogos incompletos")
            .WithDescription("No pude cargar naciones, roles o profesiones. Revisa que las tablas tengan datos activos.")
            .WithColor(Color.Red)
            .WithFooter("Thaddeus Morrowind · Personajes")
            .WithCurrentTimestamp()
            .Build();
    }

    private static string BuildDescription(
        CharacterCreationSessionDto session,
        int nationTotal,
        int roleTotal,
        int professionTotal)
    {
        StringBuilder builder = new();

        builder.AppendLine($"**Nombre:** {session.Name}");
        builder.AppendLine($"**Apodo:** {(string.IsNullOrWhiteSpace(session.Nickname) ? "Sin apodo" : session.Nickname)}");
        builder.AppendLine($"**Imagen:** {(string.IsNullOrWhiteSpace(session.ImageUrl) ? "Sin imagen personalizada" : "Imagen personalizada cargada")}");
        builder.AppendLine();
        builder.AppendLine("Usa las flechas para navegar entre opciones y confirma cuando esté listo.");
        builder.AppendLine();
        builder.AppendLine($"Progreso: Nación {session.NationIndex + 1}/{nationTotal} · Rol {session.RoleIndex + 1}/{roleTotal} · Profesión {session.ProfessionIndex + 1}/{professionTotal}");

        return builder.ToString();
    }

    private static string BuildOption(
        CharacterCatalogOptionDto option,
        int index,
        int total)
    {
        return
            $"`{index + 1}/{total}` **{option.Name}**\n" +
            $"Key: `{option.Key}`\n" +
            $"{(string.IsNullOrWhiteSpace(option.ShortDescription) ? "Sin descripción corta." : option.ShortDescription)}";
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
