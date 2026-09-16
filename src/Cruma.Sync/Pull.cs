namespace Cruma.Sync;

/// <summary>
/// Stažení změn od posledního zpracovaného pořadí změnového feedu uživatele (plan.md N-2, §5.1 krok 4).
/// Počáteční kurzor je 0.
/// </summary>
public sealed record PullRequest(long Cursor, int MaxEntries);

/// <summary>
/// Záznam změnového feedu: pořadí, typ a identifikátor entity, verze a aktuální stav entity.
/// U smazané entity je <see cref="Deleted"/> a payload chybí.
/// </summary>
public sealed record ChangeFeedEntry(
    long Sequence,
    SyncEntityType EntityType,
    Guid EntityId,
    long? Version,
    bool Deleted,
    NotePayload? Note = null,
    CategoryPayload? Category = null,
    TagPayload? Tag = null);

/// <summary>
/// Odpověď pull: záznamy s pořadím vyšším než kurzor vzestupně, nový kurzor (pořadí posledního vráceného záznamu)
/// a příznak, že čekají další záznamy.
/// </summary>
public sealed record PullResponse(IReadOnlyList<ChangeFeedEntry> Entries, long NextCursor, bool HasMore);
