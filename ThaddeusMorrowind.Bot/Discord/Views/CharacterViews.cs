using System.Text;
using Discord;
using ThaddeusMorrowind.Bot.Discord.Views.Shared;
using ThaddeusMorrowind.Bot.Features.Characters;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class CharacterViews
{
    public static Embed CreationIntro()
    {
        return new EmbedBuilder()
            .WithTitle("✨ Iniciando creación de personaje")
            .WithDescription(
                "La creación se hará mediante paneles de selección única.\n\n" +
                "Primero escribe **nombre** y **apodo**. Luego elegirás:\n" +
                "🏛️ Nación\n" +
                "🧰 Profesión\n" +
                "⚔️ Rol\n\n" +
                "Tienes unos minutos para completar cada paso.")
            .WithColor(EmbedPalette.ArcanePurple)
            .WithFooter("Thaddeus Morrowind · Creación de personaje")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed ChooseNation(string name, string nickname)
    {
        return new EmbedBuilder()
            .WithTitle("🏛️ Escoge la nación")
            .WithDescription(
                $"Personaje: **{name}**\n" +
                $"Apodo: **{nickname}**\n\n" +
                "La nación define identidad, enfoque de juego y parte del árbol de progresión.")
            .WithColor(EmbedPalette.ArcanePurple)
            .WithFooter("Paso 1 de 3 · Nación")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed ChooseProfession(CharacterCatalogOptionDto nation)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"🏛️ Nación elegida: {nation.Name}")
            .WithDescription(
                $"{nation.ShortDescription ?? "Sin resumen."}\n\n" +
                $"{nation.Description ?? "Sin descripción extendida."}\n\n" +
                "Ahora elige una **profesión**.")
            .WithColor(EmbedPalette.UserGold)
            .WithFooter("Paso 2 de 3 · Profesión")
            .WithCurrentTimestamp();

        ApplyImage(builder, nation);

        return builder.Build();
    }

    public static Embed ChooseRole(CharacterCatalogOptionDto profession)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"🧰 Profesión elegida: {profession.Name}")
            .WithDescription(
                $"{profession.ShortDescription ?? "Sin resumen."}\n\n" +
                $"{profession.Description ?? "Sin descripción extendida."}\n\n" +
                "Ahora elige el **rol de combate**.")
            .WithColor(EmbedPalette.CharacterBlue)
            .WithFooter("Paso 3 de 3 · Rol")
            .WithCurrentTimestamp();

        ApplyImage(builder, profession);

        return builder.Build();
    }

    public static Embed ConfirmRole(CharacterCatalogOptionDto role)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"⚔️ Rol elegido: {role.Name}")
            .WithDescription(
                $"{role.ShortDescription ?? "Sin resumen."}\n\n" +
                $"{role.Description ?? "Sin descripción extendida."}\n\n" +
                "Crearé el personaje con estas elecciones.")
            .WithColor(EmbedPalette.SuccessGreen)
            .WithFooter("Creación final")
            .WithCurrentTimestamp();

        ApplyImage(builder, role);

        return builder.Build();
    }

    public static Embed Created(CharacterDetailDto character)
    {
        return Detail(character)
            .WithTitle("✅ Personaje creado con éxito")
            .WithDescription(
                $"**Nombre:** {character.Name}\n" +
                $"**Apodo:** {character.Nickname ?? character.Name}\n" +
                $"**Nivel:** {character.Level}\n" +
                $"**Nación:** {character.Nation}\n" +
                $"**Profesión:** {character.Profession}\n" +
                $"**Rol:** {character.Role}\n\n" +
                "Guarda para ver la ficha con `/personaje ver` o `!pj ver`.")
            .WithColor(EmbedPalette.SuccessGreen)
            .Build();
    }

    public static Embed Selected(CharacterDetailDto character)
    {
        return Detail(character)
            .WithTitle("✅ Personaje seleccionado")
            .WithDescription($"Ahora estás jugando con **{character.Name}**.")
            .WithColor(EmbedPalette.SuccessGreen)
            .Build();
    }

    public static Embed DetailEmbed(CharacterDetailDto character)
    {
        return Detail(character)
            .WithTitle($"🧙 {character.Name}")
            .WithDescription(
                $"**Nivel {character.Level} · {character.Role}**\n" +
                $"**Nación:** {character.Nation}\n" +
                $"**Profesión:** {character.Profession}\n" +
                (string.IsNullOrWhiteSpace(character.Nickname) ? "" : $"**Apodo:** {character.Nickname}\n"))
            .Build();
    }

    public static Embed FullStats(CharacterDetailDto character)
    {
        EmbedBuilder builder = Detail(character)
            .WithTitle($"📊 Stats completos · {character.Name}")
            .WithDescription("Vista extendida de estadísticas.");

        return builder.Build();
    }

    public static Embed List(IReadOnlyList<CharacterSummaryDto> characters)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle("🧾 Lista de personajes")
            .WithColor(EmbedPalette.CharacterBlue)
            .WithFooter("Usa /personaje ver o !pj ver")
            .WithCurrentTimestamp();

        if (characters.Count == 0)
        {
            builder.WithDescription("Todavía no tienes personajes. Usa `/personaje crear` o `!pj crear`.");
            return builder.Build();
        }

        StringBuilder description = new();

        foreach (CharacterSummaryDto character in characters)
        {
            string active = character.IsActiveCharacter ? " ⭐ Activo" : "";
            string main = character.IsMainCharacter ? " 👑 Principal" : "";
            string nickname = string.IsNullOrWhiteSpace(character.Nickname) ? "" : $" · _{character.Nickname}_";

            description.AppendLine(
                $"`ID {character.Id}` · **{character.Name}**{nickname} · Nv. {character.Level} · {character.Nation}/{character.Profession}/{character.Role}{active}{main}");
        }

        builder.WithDescription(description.ToString());

        string? thumbnail = characters.FirstOrDefault(x => x.IsActiveCharacter)?.ThumbnailUrl
            ?? characters.FirstOrDefault()?.ThumbnailUrl;

        if (!string.IsNullOrWhiteSpace(thumbnail))
        {
            builder.WithThumbnailUrl(thumbnail);
        }

        return builder.Build();
    }

    public static Embed Error(string message)
    {
        return new EmbedBuilder()
            .WithTitle("⚠️ No se pudo completar la acción")
            .WithDescription(message)
            .WithColor(EmbedPalette.ErrorRed)
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed ComingSoon(string title)
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithDescription("Este apartado ya tiene botón reservado, pero se implementará en una fase posterior.")
            .WithColor(EmbedPalette.WarningOrange)
            .WithCurrentTimestamp()
            .Build();
    }

    private static EmbedBuilder Detail(CharacterDetailDto character)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithColor(character.IsActiveCharacter ? EmbedPalette.SuccessGreen : EmbedPalette.CharacterBlue)
            .WithFooter("Thaddeus Morrowind · Personajes")
            .WithCurrentTimestamp();

        string statsText = BuildMainStats(character.Stats);

        builder.AddField("Estadísticas", statsText, inline: false);

        if (!string.IsNullOrWhiteSpace(character.ThumbnailUrl))
        {
            builder.WithThumbnailUrl(character.ThumbnailUrl);
        }

        return builder;
    }

    private static string BuildMainStats(IReadOnlyList<CharacterStatDto> stats)
    {
        string Get(string key, string emoji)
        {
            CharacterStatDto? stat = stats.FirstOrDefault(x => x.Key == key);

            if (stat is null)
            {
                return $"{emoji} **{key}:** 0";
            }

            return $"{emoji} **{stat.Name}:** {FormatStat(stat)}";
        }

        return string.Join("\n", new[]
        {
            Get("vida", "❤️"),
            Get("ataque", "⚔️"),
            Get("poder_magico", "✨"),
            Get("armadura", "🛡️"),
            Get("resistencia_magica", "🔮"),
            Get("probabilidad_critica", "🎯"),
            Get("danio_critico", "💥"),
            Get("recurso_mana_maximo", "💧")
        });
    }

    private static string FormatStat(CharacterStatDto stat)
    {
        decimal value = stat.TotalValue;

        return stat.ValueKind switch
        {
            "percent" => $"{value * 100:0.##}%",
            "multiplier" => $"{value * 100:0.##}%",
            _ => $"{value:0.##}"
        };
    }

    private static void ApplyImage(EmbedBuilder builder, CharacterCatalogOptionDto option)
    {
        if (!string.IsNullOrWhiteSpace(option.IconUrl))
        {
            builder.WithThumbnailUrl(option.IconUrl);
        }

        if (!string.IsNullOrWhiteSpace(option.BannerUrl))
        {
            builder.WithImageUrl(option.BannerUrl);
        }
    }
}
