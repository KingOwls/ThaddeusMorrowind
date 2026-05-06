using Discord;
using Discord.Interactions;

namespace ThaddeusMorrowind.Bot.Discord.SlashCommands;

public sealed class ImageTestSlashCommands : InteractionModuleBase<SocketInteractionContext>
{
    private const string TestImageUrl =
        "https://i.pinimg.com/1200x/6b/5e/b3/6b5eb3b2b9347ff00853335870d139d0.jpg";

    [SlashCommand("test_imagen", "Prueba si Discord puede cargar una imagen desde URL.")]
    public async Task TestImageAsync()
    {
        Embed embed = new EmbedBuilder()
            .WithTitle("🖼️ Prueba de imagen")
            .WithDescription("Si ves la imagen abajo, Discord está cargando correctamente la URL externa.")
            .WithImageUrl(TestImageUrl)
            .WithThumbnailUrl(TestImageUrl)
            .AddField("URL usada", TestImageUrl)
            .WithColor(Color.Purple)
            .WithFooter("Thaddeus Morrowind · Test visual")
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}