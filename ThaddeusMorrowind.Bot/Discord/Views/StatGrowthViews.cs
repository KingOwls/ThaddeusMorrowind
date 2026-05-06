using System.Text;
using Discord;
using ThaddeusMorrowind.Bot.Features.Stats;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class StatGrowthViews
{
    public static Embed Result(CharacterStatGrowthResult result)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle(result.Success ? "📈 Estadísticas del personaje" : "⚠️ No se pudieron calcular estadísticas")
            .WithDescription(result.Message)
            .WithColor(result.Success ? Color.Green : Color.Red)
            .WithCurrentTimestamp();

        if (!result.Success)
        {
            return builder.Build();
        }

        builder.AddField(
            "Personaje",
            $"`ID {result.CharacterId}` · **{result.CharacterName}** · Nivel **{result.Level}**",
            inline: false);

        builder.AddField(
            "Fuentes de crecimiento",
            $"Nación: `{result.NationKey}`\nRol: `{result.RoleKey}`\nProfesión: `{result.ProfessionKey}`",
            inline: false);

        StringBuilder statsText = new();

        foreach (CharacterStatGrowthValueDto stat in result.Stats)
        {
            string total = FormatValue(stat.TotalValue, stat.ValueKind);
            string baseValue = FormatValue(stat.BaseValue, stat.ValueKind);
            string extraValue = FormatValue(stat.ExtraValue, stat.ValueKind);

            statsText.AppendLine($"**{stat.Name}:** {total}  `base {baseValue} + extra {extraValue}`");
        }

        builder.AddField(
            "Valores",
            statsText.Length == 0 ? "_No hay estadísticas cargadas._" : statsText.ToString(),
            inline: false);

        builder.WithFooter("Thaddeus Morrowind · Crecimiento por nivel");

        return builder.Build();
    }

    private static string FormatValue(decimal value, string valueKind)
    {
        string text = decimal.Round(value, 2).ToString("0.##");

        if (valueKind.Equals("percent", StringComparison.OrdinalIgnoreCase) ||
            valueKind.Equals("percentage", StringComparison.OrdinalIgnoreCase))
        {
            return $"{text}%";
        }

        return text;
    }
}
