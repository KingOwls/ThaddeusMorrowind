using Discord;
using ThaddeusMorrowind.Bot.Discord.Views.Shared;
using ThaddeusMorrowind.Bot.Features.Characters.Experience;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class CharacterExperienceViews
{
    public static Embed Result(CharacterExperienceResult result)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle(result.Success ? "✨ Experiencia actualizada" : "⚠️ No se pudo actualizar experiencia")
            .WithDescription(result.Message)
            .WithColor(result.Success ? EmbedPalette.SuccessGreen : EmbedPalette.ErrorRed)
            .WithCurrentTimestamp();

        if (!result.Success)
        {
            return builder.Build();
        }

        string delta = result.DeltaExperience >= 0
            ? $"+{result.DeltaExperience}"
            : result.DeltaExperience.ToString();

        builder
            .AddField("Personaje", $"`ID {result.CharacterId}` · **{result.CharacterName}**", inline: false)
            .AddField("Cambio", delta, inline: true)
            .AddField("Experiencia", $"{result.PreviousExperience} → **{result.NewExperience}**", inline: true)
            .AddField("Nivel", $"{result.PreviousLevel} → **{result.NewLevel}**", inline: true);

        if (result.XpNeededForNextLevel == 0)
        {
            builder.AddField("Progreso", "Nivel máximo alcanzado o sin siguiente nivel.", inline: false);
        }
        else
        {
            builder.AddField(
                "Progreso del nivel",
                $"{result.XpProgressInCurrentLevel} acumulada en este nivel\nFaltan **{result.XpNeededForNextLevel} XP** para el siguiente.",
                inline: false);
        }

        builder.WithFooter("Thaddeus Morrowind · Experiencia");

        return builder.Build();
    }
}
