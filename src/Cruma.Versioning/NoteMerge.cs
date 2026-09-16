using System.Collections.Immutable;
using Cruma.Content;
using Cruma.Notes;

namespace Cruma.Versioning;

/// <summary>Příchozí změna poznámky od klienta: stav dokumentu a vlastností, zdroj a čas změny.</summary>
public sealed record IncomingNoteChange(ContentDocument Document, Note Metadata, VersionSource Source, DateTimeOffset ChangedAtUtc);

/// <summary>Jak byla příchozí změna přijata (odpovídá výsledkům synchronizace applied / merged / conflict).</summary>
public enum NoteMergeOutcome
{
    /// <summary>Základ byl poslední verzí – změna se použila bez slučování.</summary>
    Applied,

    /// <summary>Změna se sloučila bez konfliktu.</summary>
    Merged,

    /// <summary>Výsledek obsahuje konflikt bloku.</summary>
    Conflict,
}

/// <summary>Výsledek merge poznámky; <see cref="ToVersion"/> z něj vytvoří další verzi.</summary>
public sealed record NoteMergeResult(
    NoteMergeOutcome Outcome,
    ContentDocument Document,
    Note Metadata,
    ImmutableArray<OverwrittenValue> OverwrittenValues,
    ImmutableArray<MergeNotice> Notices)
{
    /// <summary>
    /// Další verze po <paramref name="current"/>. Použitá změna nese zdroj klienta, sloučená zdroj
    /// <see cref="VersionSource.Merge"/>; základní verze je verze, ze které klient vycházel.
    /// </summary>
    public NoteVersion ToVersion(NoteVersion current, IncomingNoteChange change, long baseNumber, DateTimeOffset createdAtUtc) =>
        current.Next(
            Document,
            Metadata,
            Outcome == NoteMergeOutcome.Applied ? change.Source : VersionSource.Merge,
            createdAtUtc,
            baseNumber,
            OverwrittenValues);
}

/// <summary>
/// Merge poznámky na serveru (VER-002): dokument po blocích (<see cref="DocumentMerge"/>), vlastnosti
/// (<see cref="MetadataMerge"/>) a pravidlo smazání proti úpravě na úrovni poznámky (§3.5, VER-004).
/// </summary>
public static class NoteMerge
{
    public static NoteMergeResult Merge(NoteVersion baseVersion, NoteVersion current, IncomingNoteChange incoming)
    {
        if (baseVersion.NoteId != current.NoteId || incoming.Metadata.Id != current.NoteId)
        {
            throw new ArgumentException("Merge musí být nad jednou poznámkou.");
        }

        if (baseVersion.Number > current.Number)
        {
            throw new ArgumentException("Základní verze nesmí být novější než aktuální verze.", nameof(baseVersion));
        }

        // Základ je poslední verze: nic k slučování (§3.1).
        if (baseVersion.Number == current.Number)
        {
            return new NoteMergeResult(NoteMergeOutcome.Applied, incoming.Document, incoming.Metadata, [], []);
        }

        var document = DocumentMerge.Merge(
            baseVersion.Document,
            current.Document,
            incoming.Document,
            new ConflictOrigin(ClientType(current.Source), current.CreatedAtUtc),
            new ConflictOrigin(ClientType(incoming.Source), incoming.ChangedAtUtc));
        var metadata = MetadataMerge.Merge(baseVersion.Metadata, current.Metadata, incoming.Metadata);

        var notices = document.Notices.ToBuilder();
        var mergedMetadata = metadata.Metadata;
        var overwritten = metadata.OverwrittenValues;

        var incomingEdited = !incoming.Document.ContentEquals(baseVersion.Document)
            || !incoming.Metadata.WithState(baseVersion.Metadata.State).Equals(baseVersion.Metadata);
        var currentEdited = !current.Document.ContentEquals(baseVersion.Document)
            || !current.Metadata.WithState(baseVersion.Metadata.State).Equals(baseVersion.Metadata);
        var baseTrashed = baseVersion.Metadata.State == NoteState.Trashed;

        if (!baseTrashed && current.Metadata.State == NoteState.Trashed && mergedMetadata.State == NoteState.Trashed && incomingEdited)
        {
            // Úprava poznámky, kterou mezitím někdo přesunul do koše, ji vrátí mezi aktivní.
            mergedMetadata = mergedMetadata.WithState(NoteState.Active);
            overwritten = [.. overwritten.Where(value => value.Field != MetadataMerge.StateField)];
            notices.Add(new MergeNotice(MergeNoticeKind.NoteRestoredFromTrash));
        }
        else if (!baseTrashed && incoming.Metadata.State == NoteState.Trashed && current.Metadata.State != NoteState.Trashed && currentEdited)
        {
            notices.Add(new MergeNotice(MergeNoticeKind.NoteTrashedAfterConcurrentEdit));
        }

        return new NoteMergeResult(
            document.HasConflict ? NoteMergeOutcome.Conflict : NoteMergeOutcome.Merged,
            document.Document,
            mergedMetadata,
            overwritten,
            notices.ToImmutable());
    }

    /// <summary>Typ klienta pro původ varianty konfliktu.</summary>
    public static string ClientType(VersionSource source) => source.ToString().ToLowerInvariant();
}
