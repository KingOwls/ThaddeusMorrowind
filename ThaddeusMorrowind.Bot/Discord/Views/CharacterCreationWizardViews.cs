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
        CharacterCatalogOptionDto currentOption = CurrentOption(session, nation, role, profession);
        string currentPanelTitle = CurrentPanelTitle(session.CurrentStep);
        int currentIndex = CurrentIndex(session);
        int currentTotal = CurrentTotal(session, nationTotal, roleTotal, professionTotal);

        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"🧙 Creación de personaje · {currentPanelTitle}")
            .WithDescription(BuildDescription(session))
            .WithColor(CurrentPanelColor(session.CurrentStep))
            .AddField(currentPanelTitle, BuildCurrentOption(currentOption, currentIndex, currentTotal), inline: false)
            .AddField("Selecciones actuales", BuildCurrentSelections(nation, role, profession), inline: false)
            .WithFooter($"Sesión {session.SessionId} · Panel {session.CurrentStep + 1}/3 · Expira en 10 minutos")
            .WithCurrentTimestamp();

        // En el wizard queremos que el icono mostrado sea el del catálogo actual.
        // La imagen del personaje se muestra solo como dato del personaje, no reemplaza el icono del panel.
        string? thumbnailUrl = NormalizeImageUrl(currentOption.IconUrl);
        string? bannerUrl = NormalizeImageUrl(currentOption.BannerUrl);

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            builder.WithThumbnailUrl(thumbnailUrl);
            builder.WithAuthor(currentOption.Name, thumbnailUrl);
        }
        else
        {
            builder.WithAuthor(currentOption.Name);
        }

        if (!string.IsNullOrWhiteSpace(bannerUrl))
        {
            builder.WithImageUrl(bannerUrl);
        }
        else if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            // Si no hay banner, al menos se muestra el icono también como imagen grande.
            builder.WithImageUrl(thumbnailUrl);
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

    private static string BuildDescription(CharacterCreationSessionDto session)
    {
        StringBuilder builder = new();

        builder.AppendLine($"**Nombre:** {session.Name}");
        builder.AppendLine($"**Apodo:** {(string.IsNullOrWhiteSpace(session.Nickname) ? "Sin apodo" : session.Nickname)}");
        builder.AppendLine($"**Imagen del personaje:** {(string.IsNullOrWhiteSpace(session.ImageUrl) ? "No configurada" : "Configurada")}");
        builder.AppendLine();
        builder.AppendLine("Usa **◀** y **▶** para navegar. Cuando te guste la opción, pulsa aceptar.");
        builder.AppendLine();
        builder.AppendLine(session.CurrentStep switch
        {
            0 => "Ahora estás escogiendo la **Nación**. Define identidad, afinidad narrativa y parte del crecimiento.",
            1 => "Ahora estás escogiendo el **Rol**. Define función de combate y estilo principal.",
            2 => "Ahora estás escogiendo la **Profesión**. Define utilidad, exploración y recursos.",
            _ => "Ahora estás escogiendo una opción."
        });

        return builder.ToString();
    }

    private static string BuildCurrentOption(
        CharacterCatalogOptionDto option,
        int index,
        int total)
    {
        return
            $"`{index + 1}/{total}` **{option.Name}**\n" +
            $"Key: `{option.Key}`\n\n" +
            $"{(string.IsNullOrWhiteSpace(option.ShortDescription) ? "Sin descripción corta." : option.ShortDescription)}\n\n" +
            $"{(string.IsNullOrWhiteSpace(option.Description) ? "Descripción extendida pendiente." : option.Description)}";
    }

    private static string BuildCurrentSelections(
        CharacterCatalogOptionDto nation,
        CharacterCatalogOptionDto role,
        CharacterCatalogOptionDto profession)
    {
        return
            $"🏳️ Nación: **{nation.Name}** `key: {nation.Key}`\n" +
            $"⚔️ Rol: **{role.Name}** `key: {role.Key}`\n" +
            $"🧰 Profesión: **{profession.Name}** `key: {profession.Key}`";
    }

    private static CharacterCatalogOptionDto CurrentOption(
        CharacterCreationSessionDto session,
        CharacterCatalogOptionDto nation,
        CharacterCatalogOptionDto role,
        CharacterCatalogOptionDto profession)
    {
        return session.CurrentStep switch
        {
            0 => nation,
            1 => role,
            2 => profession,
            _ => nation
        };
    }

    private static string CurrentPanelTitle(int currentStep)
    {
        return currentStep switch
        {
            0 => "🏳️ Nación",
            1 => "⚔️ Rol",
            2 => "🧰 Profesión",
            _ => "📚 Selección"
        };
    }

    private static Color CurrentPanelColor(int currentStep)
    {
        return currentStep switch
        {
            0 => Color.Purple,
            1 => Color.DarkRed,
            2 => Color.DarkGreen,
            _ => Color.Blue
        };
    }

    private static int CurrentIndex(CharacterCreationSessionDto session)
    {
        return session.CurrentStep switch
        {
            0 => session.NationIndex,
            1 => session.RoleIndex,
            2 => session.ProfessionIndex,
            _ => 0
        };
    }

    private static int CurrentTotal(
        CharacterCreationSessionDto session,
        int nationTotal,
        int roleTotal,
        int professionTotal)
    {
        return session.CurrentStep switch
        {
            0 => nationTotal,
            1 => roleTotal,
            2 => professionTotal,
            _ => nationTotal
        };
    }

    private static string? NormalizeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Discord suele tener problemas mostrando SVG en thumbnails/embeds.
        // Si viene de DiceBear, usar PNG ayuda a que el icono sea visible.
        return url.Replace("/svg?", "/png?", StringComparison.OrdinalIgnoreCase);
    }
}
