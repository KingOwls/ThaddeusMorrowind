using System.Globalization;
using System.Text;

namespace ThaddeusMorrowind.Bot.Common.Helpers;

public static class GameKeyNormalizer
{
    public static string NormalizeKey(string value)
    {
        string trimmed = value.Trim().ToLowerInvariant();
        string normalized = trimmed.Normalize(NormalizationForm.FormD);

        StringBuilder builder = new();

        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                builder.Append('_');
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
