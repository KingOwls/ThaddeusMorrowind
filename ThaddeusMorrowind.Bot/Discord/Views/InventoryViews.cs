using Discord;
using System.Text;
using ThaddeusMorrowind.Bot.Features.Inventory.Dtos;

namespace ThaddeusMorrowind.Bot.Discord.Views;

public static class InventoryViews
{
    public static Embed Result(string title, string message, bool success)
    {
        return new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .WithColor(success ? Color.Green : Color.Red)
            .WithFooter("Thaddeus Morrowind · Inventario")
            .WithCurrentTimestamp()
            .Build();
    }

    public static Embed Inventory(string title, IReadOnlyList<InventoryItemDto> items)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle(title)
            .WithColor(Color.Blue)
            .WithFooter($"Total visible: {items.Count} · Thaddeus Morrowind")
            .WithCurrentTimestamp();

        if (items.Count == 0)
        {
            builder.WithDescription("No hay objetos en este inventario.");
            return builder.Build();
        }

        StringBuilder description = new();

        foreach (InventoryItemDto item in items.Take(20))
        {
            description.AppendLine($"`ID {item.ItemInstanceId}` **{item.Name}** x{item.Quantity} {Stars(item.Stars)}");
            description.AppendLine($"Categoría: `{item.CategoryKey}` · Tipo: `{item.ItemSubtype ?? "general"}` · Lugar: `{item.Location}`");

            if (!string.IsNullOrWhiteSpace(item.ShortDescription))
            {
                description.AppendLine(item.ShortDescription);
            }

            description.AppendLine();
        }

        if (items.Count > 20)
        {
            description.AppendLine($"...y {items.Count - 20} objetos más.");
        }

        builder.WithDescription(description.ToString());

        InventoryItemDto? firstWithIcon = items.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.IconUrl));

        if (firstWithIcon is not null)
        {
            builder.WithThumbnailUrl(firstWithIcon.IconUrl);
        }

        return builder.Build();
    }

    public static Embed Equipment(EquipmentSummaryDto summary)
    {
        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle($"🎒 Equipo de {summary.CharacterName}")
            .WithColor(Color.Purple)
            .WithFooter($"ID personaje {summary.CharacterId} · Equipo")
            .WithCurrentTimestamp();

        if (!string.IsNullOrWhiteSpace(summary.CharacterImageUrl))
        {
            builder.WithThumbnailUrl(summary.CharacterImageUrl);
        }

        StringBuilder description = new();

        foreach (EquipmentSlotDto slot in summary.Slots)
        {
            if (slot.Item is null)
            {
                description.AppendLine($"**{slot.SlotName}:** vacío `slot: {slot.SlotKey}`");
            }
            else
            {
                description.AppendLine($"**{slot.SlotName}:** {slot.Item.Name} {Stars(slot.Item.Stars)} `ID {slot.Item.ItemInstanceId}`");
            }
        }

        builder.WithDescription(description.ToString());

        int setPieces = summary.Slots.Count(slot => slot.CountsForArtifactSet && slot.Item is not null);

        builder.AddField(
            "Sets de artefactos",
            $"Piezas que cuentan para set: **{setPieces}/5**\nLa herramienta única no cuenta para set.",
            inline: false);

        return builder.Build();
    }

    public static Embed Give(DebugGiveItemDto dto)
    {
        return new EmbedBuilder()
            .WithTitle("🎁 Item otorgado")
            .WithDescription($"Usuario: **{dto.OwnerUsername}**\nItem: **{dto.ItemName}** x{dto.Quantity}\nInstancia: `ID {dto.ItemInstanceId}`")
            .WithColor(Color.Green)
            .WithFooter("Comando admin/debug")
            .WithCurrentTimestamp()
            .Build();
    }

    private static string Stars(int? stars)
    {
        if (stars is null || stars.Value <= 0)
        {
            return string.Empty;
        }

        return new string('★', stars.Value);
    }
}
