using Discord.Interactions;
using DTextInputStyle = Discord.TextInputStyle;

namespace ThaddeusMorrowind.Bot.Discord.Modals;

public sealed class CreateCharacterModal : IModal
{
    public string Title => "Crear personaje";

    [InputLabel("Nombre del personaje")]
    [ModalTextInput(
        "character_name",
        DTextInputStyle.Short,
        "Ejemplo: Suki",
        minLength: 2,
        maxLength: 80)]
    public string Name { get; set; } = string.Empty;

    [InputLabel("Apodo")]
    [ModalTextInput(
        "character_nickname",
        DTextInputStyle.Short,
        "Ejemplo: Ermitaña",
        minLength: 1,
        maxLength: 80)]
    public string Nickname { get; set; } = string.Empty;
}