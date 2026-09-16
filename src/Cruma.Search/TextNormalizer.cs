using System.Globalization;
using System.Text;

namespace Cruma.Search;

/// <summary>
/// Normalizace textu pro vyhledávání (search-pattern.md §2, SRC-001): Unicode NFC, sjednocení velikosti písmen
/// nezávislé na kultuře a odstranění diakritiky. Stejná normalizace pro indexovaný text i dotaz (SRC-003).
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Verze normalizace a tokenizace. Každá změna chování ji zvyšuje a vyvolá přeindexování na desktopu i serveru
    /// (SRC-002).
    /// </summary>
    public const int Version = 1;

    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // 1. NFC, 2. malá písmena bez závislosti na kultuře.
        var folded = text.Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // 3. Rozklad na NFD, odstranění kombinujících znaků, složení zpět.
        var decomposed = folded.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
