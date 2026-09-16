using System.Collections.Immutable;

namespace Cruma.Search;

/// <summary>
/// Vyhledávací dotaz (search-pattern.md §1): dotaz projde stejnou normalizací a tokenizací jako indexovaný text
/// a každý token se hledá jako začátek některého tokenu textu; všechny tokeny musí odpovídat (SRC-003).
/// </summary>
public sealed record SearchQuery
{
    private SearchQuery(ImmutableArray<string> tokens) => Tokens = tokens;

    /// <summary>Normalizované tokeny dotazu, bez duplicit, v pořadí zadání.</summary>
    public ImmutableArray<string> Tokens { get; }

    /// <summary>Dotaz bez tokenů (prázdný nebo jen oddělovače) nic nenajde.</summary>
    public bool IsEmpty => Tokens.IsEmpty;

    public static SearchQuery Parse(string? query) => new([.. Tokenizer.TokenizeText(query).Distinct(StringComparer.Ordinal)]);

    /// <summary>
    /// Referenční vyhodnocení nad tokeny textu. Adaptéry indexu musí vracet stejnou množinu výsledků (TST-002).
    /// </summary>
    public bool Matches(IReadOnlyCollection<string> textTokens) =>
        !IsEmpty && Tokens.All(queryToken => textTokens.Any(token => token.StartsWith(queryToken, StringComparison.Ordinal)));

    /// <summary>Referenční vyhodnocení nad textem, který se nejdřív normalizuje a tokenizuje.</summary>
    public bool Matches(string? text) => Matches(Tokenizer.TokenizeText(text));
}
