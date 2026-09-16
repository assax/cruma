using System.Collections.Immutable;
using System.Globalization;

namespace Cruma.Search;

/// <summary>
/// Tokenizace normalizovaného textu (search-pattern.md §3, SRC-001): dělí na všem, co není písmeno ani číslice;
/// bez stop slov a bez stemmingu (NFR-5).
/// </summary>
public static class Tokenizer
{
    /// <summary>Tokeny normalizovaného textu v pořadí výskytu.</summary>
    public static ImmutableArray<string> Tokenize(string? normalizedText)
    {
        if (string.IsNullOrEmpty(normalizedText))
        {
            return [];
        }

        var tokens = ImmutableArray.CreateBuilder<string>();
        var start = -1;
        var index = 0;
        while (index < normalizedText.Length)
        {
            var length = char.IsSurrogatePair(normalizedText, index) ? 2 : 1;
            if (IsTokenCharacter(normalizedText, index))
            {
                if (start < 0)
                {
                    start = index;
                }
            }
            else if (start >= 0)
            {
                tokens.Add(normalizedText[start..index]);
                start = -1;
            }

            index += length;
        }

        if (start >= 0)
        {
            tokens.Add(normalizedText[start..]);
        }

        return tokens.ToImmutable();
    }

    /// <summary>Normalizuje a tokenizuje text pro index.</summary>
    public static ImmutableArray<string> TokenizeText(string? text) => Tokenize(TextNormalizer.Normalize(text));

    /// <summary>Kanonický řetězec tokenů oddělených jednou mezerou pro uložení v indexu (§3).</summary>
    public static string ToIndexString(IEnumerable<string> tokens) => string.Join(' ', tokens);

    private static bool IsTokenCharacter(string text, int index)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(text, index);
        return category is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.DecimalDigitNumber;
    }
}
