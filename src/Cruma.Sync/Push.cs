namespace Cruma.Sync;

/// <summary>
/// Jedna čekající změna desktopu (versioning-and-sync-pattern.md §5.1 krok 3). <see cref="ChangeId"/> generuje
/// klient a server ji zpracuje nejvýš jednou (SYN-003); <see cref="BaseVersion"/> je verze, ze které změna vznikla,
/// server bez merge proti ní změnu nepoužije (SYN-002). Nová entita základní verzi nemá.
/// Vyplněný je právě jeden payload odpovídající <see cref="EntityType"/>; u smazání žádný.
/// </summary>
public sealed record SyncChange(
    Guid ChangeId,
    SyncEntityType EntityType,
    Guid EntityId,
    SyncOperation Operation,
    long? BaseVersion,
    DateTimeOffset ChangedAtUtc,
    NotePayload? Note = null,
    CategoryPayload? Category = null,
    TagPayload? Tag = null);

public sealed record PushRequest(IReadOnlyList<SyncChange> Changes);

/// <summary>Výsledek zpracování změny serverem.</summary>
public enum ChangeOutcome
{
    /// <summary>Změna se použila jako další verze.</summary>
    Applied,

    /// <summary>Změna se sloučila s mezitím vzniklou verzí.</summary>
    Merged,

    /// <summary>Sloučená verze obsahuje konflikt.</summary>
    Conflict,

    /// <summary>Změna odmítnuta; na klientovi zůstává čekající a stav synchronizace je chyba (ERR-002).</summary>
    Rejected,
}

/// <summary>Přepsaná skalární vlastnost, kterou stav synchronizace ohlásí (VER-005).</summary>
public sealed record OverwrittenField(string Field, string? OverwrittenValue, string? WinningValue);

/// <summary>
/// Výsledek jedné změny. <see cref="Version"/> je verze vzniklá na serveru (u zamítnutí chybí),
/// <see cref="ErrorCode"/> je stabilní kód u zamítnutí.
/// </summary>
public sealed record ChangeResult(
    Guid ChangeId,
    ChangeOutcome Outcome,
    long? Version,
    string? ErrorCode = null,
    IReadOnlyList<OverwrittenField>? OverwrittenFields = null);

public sealed record PushResponse(IReadOnlyList<ChangeResult> Results);
