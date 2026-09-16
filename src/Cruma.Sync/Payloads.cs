using Cruma.Notes;

namespace Cruma.Sync;

/// <summary>Typ synchronizované entity v I-1.</summary>
public enum SyncEntityType
{
    Note,
    Category,
    Tag,
}

/// <summary>Operace změny.</summary>
public enum SyncOperation
{
    /// <summary>Vytvoření nebo úprava (u poznámky včetně přesunu do koše a obnovení – to jsou stavy).</summary>
    Upsert,

    /// <summary>Smazání kategorie nebo štítku, trvalé smazání poznámky z koše (VER-008).</summary>
    Delete,
}

/// <summary>
/// Stav poznámky přenášený protokolem: vlastnosti a dokument jako JSON schématu Cruma.Content (CNT-006).
/// </summary>
public sealed record NotePayload(
    string? Title,
    Guid CategoryId,
    IReadOnlyList<Guid> TagIds,
    string? Color,
    bool IsPinned,
    NoteState State,
    string DocumentJson);

public sealed record CategoryPayload(string Name, bool IsDefault);

public sealed record TagPayload(string Name);
