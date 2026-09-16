using System.Collections.Immutable;

namespace Cruma.Search;

/// <summary>Položka indexu: entita a její tokeny z <see cref="Tokenizer"/> (search-pattern.md §1, §5).</summary>
/// <param name="EntityType">Typ entity, v I-1 jen <see cref="SearchEntityTypes.Note"/>.</param>
/// <param name="State">Stav entity pro filtr (u poznámky aktivní / archiv / koš).</param>
/// <param name="Tokens">Tokeny textu; adaptér je jen ukládá, nenormalizuje (SRC-001).</param>
public sealed record SearchIndexEntry(string EntityType, Guid EntityId, string State, ImmutableArray<string> Tokens)
{
    /// <summary>Položka z prostého textu entity – normalizace a tokenizace proběhne zde.</summary>
    public static SearchIndexEntry FromText(string entityType, Guid entityId, string state, string? text) =>
        new(entityType, entityId, state, Tokenizer.TokenizeText(text));
}

/// <summary>Dotaz na index; <see cref="State"/> <c>null</c> znamená bez filtru stavu.</summary>
public sealed record SearchRequest(SearchQuery Query, string EntityType, string? State, int Skip, int Take);

/// <summary>Stránka výsledků: identifikátory entit v pořadí relevance a celkový počet shod.</summary>
public sealed record SearchResultPage(IReadOnlyList<Guid> EntityIds, int TotalCount);

public static class SearchEntityTypes
{
    public const string Note = "note";
}

/// <summary>
/// Adaptér úložiště indexu (SQLite na desktopu, PostgreSQL na serveru). Ukládá a porovnává jen tokeny z Cruma.Search;
/// na serveru omezuje každý dotaz na aktuálního uživatele uvnitř adaptéru (SRC-004). Oba adaptéry musí vracet
/// stejnou množinu výsledků (TST-002).
/// </summary>
public interface ISearchIndexAdapter
{
    Task UpsertAsync(SearchIndexEntry entry, CancellationToken cancellationToken);

    Task RemoveAsync(string entityType, Guid entityId, CancellationToken cancellationToken);

    Task<SearchResultPage> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
}
