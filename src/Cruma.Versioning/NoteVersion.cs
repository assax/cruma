using System.Collections.Immutable;
using Cruma.Content;
using Cruma.Notes;

namespace Cruma.Versioning;

/// <summary>
/// Verze poznámky (versioning-and-sync-pattern.md §2.2): vzestupné číslo, základní verze, čas vzniku, zdroj,
/// dokument a snímek vlastností. Čas se předává zvenku – model nepracuje s hodinami ani náhodou (VER-001).
/// </summary>
public sealed record NoteVersion
{
    private NoteVersion(
        long number,
        long? baseNumber,
        DateTimeOffset createdAtUtc,
        VersionSource source,
        ContentDocument document,
        Note metadata,
        ImmutableArray<OverwrittenValue> overwrittenValues)
    {
        Number = number;
        BaseNumber = baseNumber;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        Source = source;
        Document = document;
        Metadata = metadata;
        OverwrittenValues = overwrittenValues;
    }

    public Guid NoteId => Metadata.Id;

    /// <summary>Číslo verze; přiděluje jen server, v rámci poznámky vzestupně od 1.</summary>
    public long Number { get; }

    /// <summary>Verze, ze které tato verze vznikla; první verze základ nemá.</summary>
    public long? BaseNumber { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public VersionSource Source { get; }

    public ContentDocument Document { get; }

    public Note Metadata { get; }

    public ImmutableArray<OverwrittenValue> OverwrittenValues { get; }

    /// <summary>Dokument obsahuje nevyřešený konflikt (versioning-and-sync-pattern.md §3.3).</summary>
    public bool HasConflict => Document.Blocks.Any(block => block.Type == ContentTypes.Conflict);

    public static NoteVersion First(Note metadata, ContentDocument document, VersionSource source, DateTimeOffset createdAtUtc) =>
        new(1, null, createdAtUtc, source, document, metadata, []);

    /// <summary>Obnoví uloženou verzi z úložiště; čísla a základ se přebírají tak, jak byly přiděleny.</summary>
    public static NoteVersion FromStored(
        long number,
        long? baseNumber,
        DateTimeOffset createdAtUtc,
        VersionSource source,
        ContentDocument document,
        Note metadata,
        IEnumerable<OverwrittenValue> overwrittenValues)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        return new NoteVersion(number, baseNumber, createdAtUtc, source, document, metadata, [.. overwrittenValues]);
    }

    /// <summary>Další verze poznámky. Předchozí verze zůstává beze změny (VER-007).</summary>
    public NoteVersion Next(
        ContentDocument document,
        Note metadata,
        VersionSource source,
        DateTimeOffset createdAtUtc,
        long? baseNumber = null,
        IEnumerable<OverwrittenValue>? overwrittenValues = null)
    {
        if (metadata.Id != NoteId)
        {
            throw new ArgumentException("Verze musí patřit téže poznámce.", nameof(metadata));
        }

        if (baseNumber is { } requestedBase && (requestedBase < 1 || requestedBase > Number))
        {
            throw new ArgumentOutOfRangeException(nameof(baseNumber), "Základní verze musí být existující verze poznámky.");
        }

        return new NoteVersion(Number + 1, baseNumber ?? Number, createdAtUtc, source, document, metadata, [.. overwrittenValues ?? []]);
    }

    /// <summary>Obnovení starší verze vytvoří novou verzi se zdrojem <see cref="VersionSource.Restore"/> (VER-007).</summary>
    public NoteVersion RestoreFrom(NoteVersion older, DateTimeOffset createdAtUtc)
    {
        if (older.NoteId != NoteId || older.Number >= Number)
        {
            throw new ArgumentException("Obnovit jde jen starší verzi téže poznámky.", nameof(older));
        }

        return Next(older.Document, older.Metadata, VersionSource.Restore, createdAtUtc, baseNumber: older.Number);
    }
}
