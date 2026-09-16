using Cruma.Content;
using Cruma.Desktop.Storage.Persistence;
using Cruma.Desktop.Storage.Search;
using Cruma.Search;
using Cruma.Sync;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Desktop.Storage;

/// <summary>Čekající změna připravená k odeslání.</summary>
public sealed record OutgoingChange(long Sequence, SyncChange Change);

/// <summary>Podpora synchronizace (versioning-and-sync-pattern.md §5.1): log čekajících změn, potvrzení, stažené změny, kurzor.</summary>
public sealed partial class LocalNotesStore
{
    /// <summary>Čekající změny v pořadí vzniku s obsahem sestaveným z aktuálního lokálního stavu; označí je jako odeslané.</summary>
    public async Task<IReadOnlyList<OutgoingChange>> TakeOutgoingChangesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var pending = await db.PendingChanges.OrderBy(change => change.Sequence).ToListAsync(cancellationToken);
        var outgoing = new List<OutgoingChange>();
        var now = time.GetUtcNow();

        foreach (var change in pending)
        {
            var type = Enum.Parse<SyncEntityType>(change.EntityType);
            var operation = Enum.Parse<SyncOperation>(change.Operation);
            var syncChange = new SyncChange(change.ChangeId, type, change.EntityId, operation, null, change.ChangedAtUtc);

            if (operation == SyncOperation.Upsert)
            {
                switch (type)
                {
                    case SyncEntityType.Note when await db.Notes.AsNoTracking().SingleOrDefaultAsync(note => note.Id == change.EntityId, cancellationToken) is { } note:
                        // Základ změny je poslední verze známá ze serveru (SYN-002); nová poznámka základ nemá.
                        syncChange = syncChange with
                        {
                            BaseVersion = note.ServerVersion,
                            Note = new NotePayload(note.Title, note.CategoryId, [.. note.TagIds], note.Color, note.IsPinned, Enum.Parse<Notes.NoteState>(note.State), note.Document),
                        };
                        break;
                    case SyncEntityType.Category when await db.Categories.AsNoTracking().SingleOrDefaultAsync(category => category.Id == change.EntityId, cancellationToken) is { } category:
                        syncChange = syncChange with { Category = new CategoryPayload(category.Name, category.IsDefault) };
                        break;
                    case SyncEntityType.Tag when await db.Tags.AsNoTracking().SingleOrDefaultAsync(tag => tag.Id == change.EntityId, cancellationToken) is { } tag:
                        syncChange = syncChange with { Tag = new TagPayload(tag.Name) };
                        break;
                    default:
                        // Entita mezitím zmizela a čeká na ni změna smazání – upsert už nemá co poslat.
                        db.PendingChanges.Remove(change);
                        continue;
                }
            }

            change.SentAtUtc ??= now;
            outgoing.Add(new OutgoingChange(change.Sequence, syncChange));
        }

        await db.SaveChangesAsync(cancellationToken);
        return outgoing;
    }

    /// <summary>
    /// Zpracuje výsledek změny ze serveru. Potvrzená změna se odebere a u poznámky se zapamatuje verze serveru; když
    /// pro poznámku nic dalšího nečeká, smažou se její lokální mezilehlé verze (SYN-004). Zamítnutá změna zůstane
    /// čekat s kódem chyby a jde dál upravovat (ERR-002).
    /// </summary>
    public async Task ApplyResultAsync(OutgoingChange outgoing, ChangeResult result, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var pending = await db.PendingChanges.SingleOrDefaultAsync(change => change.Sequence == outgoing.Sequence, cancellationToken);
        if (pending is null)
        {
            return;
        }

        var change = outgoing.Change;
        var alreadyGone = change.Operation == SyncOperation.Delete && result.ErrorCode == LocalErrorCodes.NotFound;
        if (result.Outcome == ChangeOutcome.Rejected && !alreadyGone)
        {
            pending.LastErrorCode = result.ErrorCode;
            pending.SentAtUtc = null;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        db.PendingChanges.Remove(pending);
        if (change.EntityType == SyncEntityType.Note)
        {
            if (result.Version is { } version && await db.Notes.FindAsync([change.EntityId], cancellationToken) is { } note)
            {
                note.ServerVersion = Math.Max(note.ServerVersion ?? 0, version);
            }

            var typeName = SyncEntityType.Note.ToString();
            if (!await db.PendingChanges.AnyAsync(other => other.EntityType == typeName && other.EntityId == change.EntityId && other.Sequence != pending.Sequence, cancellationToken))
            {
                await db.LocalVersions.Where(localVersion => localVersion.NoteId == change.EntityId).ExecuteDeleteAsync(cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Použije změnu staženou ze serveru. Entita s čekající lokální změnou se nepřepíše – lokální práce se nejdřív odešle
    /// a server ji sloučí (ERR-002, NFR-4).
    /// </summary>
    public Task ApplyPulledAsync(ChangeFeedEntry entry, CancellationToken cancellationToken = default) =>
        ApplyPulledPageAsync([entry], cancellationToken);

    /// <summary>Použije stránku stažených změn v jedné transakci (první synchronizace desítek tisíc poznámek, NFR-7).</summary>
    public async Task ApplyPulledPageAsync(IReadOnlyList<ChangeFeedEntry> entries, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var pendingKeys = (await db.PendingChanges.AsNoTracking().Select(change => new { change.EntityType, change.EntityId }).ToListAsync(cancellationToken))
            .Select(change => (change.EntityType, change.EntityId))
            .ToHashSet();

        foreach (var entry in entries)
        {
            if (!pendingKeys.Contains((entry.EntityType.ToString(), entry.EntityId)))
            {
                await ApplyPulledEntryAsync(db, entry, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ApplyPulledEntryAsync(CrumaLocalDbContext db, ChangeFeedEntry entry, CancellationToken cancellationToken)
    {
        switch (entry.EntityType)
        {
            case SyncEntityType.Note:
                await ApplyPulledNoteAsync(db, entry, cancellationToken);
                break;
            case SyncEntityType.Category:
                var category = await db.Categories.FindAsync([entry.EntityId], cancellationToken);
                if (entry.Deleted || entry.Category is null)
                {
                    if (category is not null)
                    {
                        db.Categories.Remove(category);
                    }
                }
                else
                {
                    category ??= db.Categories.Add(new LocalCategoryEntity { Id = entry.EntityId }).Entity;
                    category.Name = entry.Category.Name;
                    category.IsDefault = entry.Category.IsDefault;
                }

                break;
            default:
                var tag = await db.Tags.FindAsync([entry.EntityId], cancellationToken);
                if (entry.Deleted || entry.Tag is null)
                {
                    if (tag is not null)
                    {
                        db.Tags.Remove(tag);
                    }
                }
                else
                {
                    tag ??= db.Tags.Add(new LocalTagEntity { Id = entry.EntityId }).Entity;
                    tag.Name = entry.Tag.Name;
                }

                break;
        }
    }

    public async Task<long> GetCursorAsync(CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        return (await db.SyncState.AsNoTracking().SingleOrDefaultAsync(cancellationToken))?.Cursor ?? 0;
    }

    public async Task SetCursorAsync(long cursor, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var state = await db.SyncState.SingleOrDefaultAsync(cancellationToken) ?? db.SyncState.Add(new SyncStateEntity()).Entity;
        state.Cursor = cursor;
        state.LastSyncedAtUtc = time.GetUtcNow();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Souhrn pro stav synchronizace (SYN-007).</summary>
    public async Task<(int Pending, int Rejected, bool HasConflicts)> GetSyncSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var pending = await db.PendingChanges.CountAsync(cancellationToken);
        var rejected = await db.PendingChanges.CountAsync(change => change.LastErrorCode != null, cancellationToken);
        var conflicts = await db.Notes.AnyAsync(note => note.HasConflict, cancellationToken);
        return (pending, rejected, conflicts);
    }

    private async Task ApplyPulledNoteAsync(CrumaLocalDbContext db, ChangeFeedEntry entry, CancellationToken cancellationToken)
    {
        var note = await db.Notes.FindAsync([entry.EntityId], cancellationToken);
        if (entry.Deleted || entry.Note is null)
        {
            if (note is not null)
            {
                db.Notes.Remove(note);
                await db.LocalVersions.Where(version => version.NoteId == entry.EntityId).ExecuteDeleteAsync(cancellationToken);
                await new SqliteSearchIndexAdapter(db).RemoveAsync(SearchEntityTypes.Note, entry.EntityId, cancellationToken);
            }

            return;
        }

        var payload = entry.Note;
        if (note is null)
        {
            note = db.Notes.Add(new LocalNoteEntity { Id = entry.EntityId, CreatedAtUtc = time.GetUtcNow() }).Entity;
        }

        var document = ContentDocument.Parse(payload.DocumentJson);
        Apply(note, Notes.Note.Restore(entry.EntityId, payload.Title, payload.CategoryId, payload.TagIds, payload.Color, payload.IsPinned, payload.State), document, time.GetUtcNow());
        note.ServerVersion = entry.Version;
        await IndexAsync(db, note, cancellationToken);
    }
}
