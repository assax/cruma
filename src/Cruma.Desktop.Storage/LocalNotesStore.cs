using System.Text.Json;
using System.Text.Json.Serialization;
using Cruma.Content;
using Cruma.Desktop.Storage.Persistence;
using Cruma.Desktop.Storage.Search;
using Cruma.Notes;
using Cruma.Search;
using Cruma.Sync;
using Cruma.Versioning;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Desktop.Storage;

/// <summary>
/// Lokální zápisová cesta desktopu (desktop-pattern.md §2): každá úprava v jedné transakci uloží poznámku, lokální
/// verzi editační relace, čekající změnu a index vyhledávání. Doménová pravidla jsou v Cruma.Notes a Cruma.Versioning.
/// Funguje bez sítě (FR-26 akc. 1).
/// </summary>
public sealed partial class LocalNotesStore(LocalDatabase database, TimeProvider time, LocalStoreOptions options)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public async Task<StoreResult<LocalNote>> CreateNoteAsync(Guid id, ContentDocument document, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (await db.Notes.FindAsync([id], cancellationToken) is { } existing)
        {
            return StoreResult<LocalNote>.Success(ToLocal(existing));
        }

        var defaultCategory = await db.Categories.SingleOrDefaultAsync(category => category.IsDefault, cancellationToken);
        if (defaultCategory is null)
        {
            return StoreResult<LocalNote>.Failure(LocalErrorCodes.DefaultCategoryMissing, "Výchozí kategorie ještě není stažená ze serveru.");
        }

        // Nová poznámka patří do výchozí kategorie a nic dalšího nevyžaduje (FR-1, UI-005).
        var metadata = Note.Create(id, Category.Restore(defaultCategory.Id, defaultCategory.Name, isDefault: true));
        var now = time.GetUtcNow();
        var entity = new LocalNoteEntity { Id = id, CreatedAtUtc = now };
        Apply(entity, metadata, document, now);
        db.Notes.Add(entity);

        await RecordLocalVersionAsync(db, entity, now, cancellationToken);
        await EnqueueAsync(db, SyncEntityType.Note, id, SyncOperation.Upsert, now, cancellationToken);
        await IndexAsync(db, entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StoreResult<LocalNote>.Success(ToLocal(entity));
    }

    public Task<StoreResult<LocalNote>> SaveNoteAsync(Guid id, LocalNoteChanges changes, CancellationToken cancellationToken = default) =>
        UpdateNoteAsync(id, async (db, current, document) =>
        {
            if (!await db.Categories.AnyAsync(category => category.Id == changes.CategoryId, cancellationToken))
            {
                return (null, null, LocalErrorCodes.CategoryNotFound);
            }

            var tagIds = changes.TagIds.Distinct().ToList();
            if (await db.Tags.CountAsync(tag => tagIds.Contains(tag.Id), cancellationToken) != tagIds.Count)
            {
                return (null, null, LocalErrorCodes.TagNotFound);
            }

            var metadata = Note.Restore(id, changes.Title, changes.CategoryId, tagIds, changes.Color, changes.IsPinned, current.State);
            return (metadata, changes.Document, null);
        }, cancellationToken);

    public Task<StoreResult<LocalNote>> ChangeStateAsync(Guid id, LocalStateChange change, CancellationToken cancellationToken = default) =>
        UpdateNoteAsync(id, (_, current, document) =>
        {
            var changed = change switch
            {
                LocalStateChange.Archive => current.Archive(),
                LocalStateChange.Unarchive => current.Unarchive(),
                LocalStateChange.Trash => current.MoveToTrash(),
                _ => current.RestoreFromTrash(),
            };
            return Task.FromResult<(Note?, ContentDocument?, string?)>((changed, document, null));
        }, cancellationToken);

    /// <summary>Vyřeší konflikt lokálně; výsledek se odešle jako běžná změna a server ho sloučí (FR-28 akc. 3).</summary>
    public Task<StoreResult<LocalNote>> ResolveConflictAsync(Guid id, string blockId, ConflictSide? side, ContentBlock? editedBlock, CancellationToken cancellationToken = default) =>
        UpdateNoteAsync(id, (_, current, document) =>
        {
            try
            {
                var resolved = side is { } chosen
                    ? ConflictResolution.ChooseVariant(document, blockId, chosen)
                    : ConflictResolution.ReplaceWithEdit(document, blockId, editedBlock ?? throw new ArgumentException("Chybí upravený blok."));
                return Task.FromResult<(Note?, ContentDocument?, string?)>((current, resolved, null));
            }
            catch (ArgumentException)
            {
                return Task.FromResult<(Note?, ContentDocument?, string?)>((null, null, LocalErrorCodes.ConflictResolutionInvalid));
            }
        }, cancellationToken);

    public async Task<StoreResult<bool>> DeleteNotePermanentlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entity = await db.Notes.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return StoreResult<bool>.Failure(LocalErrorCodes.NotFound, "Poznámka neexistuje.");
        }

        try
        {
            ToNote(entity).EnsureCanBeDeletedPermanently();
        }
        catch (NotesRuleException exception)
        {
            return StoreResult<bool>.Failure(exception.Code, exception.Message);
        }

        db.Notes.Remove(entity);
        await db.LocalVersions.Where(version => version.NoteId == id).ExecuteDeleteAsync(cancellationToken);
        await new SqliteSearchIndexAdapter(db).RemoveAsync(SearchEntityTypes.Note, id, cancellationToken);
        await EnqueueAsync(db, SyncEntityType.Note, id, SyncOperation.Delete, time.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StoreResult<bool>.Success(true);
    }

    public async Task<StoreResult<LocalNote>> GetNoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var entity = await db.Notes.AsNoTracking().SingleOrDefaultAsync(note => note.Id == id, cancellationToken);
        return entity is null ? StoreResult<LocalNote>.Failure(LocalErrorCodes.NotFound, "Poznámka neexistuje.") : StoreResult<LocalNote>.Success(ToLocal(entity));
    }

    public async Task<LocalPage<LocalNote>> ListNotesAsync(NoteState state, Guid? categoryId, Guid? tagId, int skip, int take, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var stateName = state.ToString();
        var notes = db.Notes.AsNoTracking().Where(note => note.State == stateName);
        if (categoryId is { } category)
        {
            notes = notes.Where(note => note.CategoryId == category);
        }

        var all = await notes.ToListAsync(cancellationToken);

        // Štítky jsou v JSON sloupci, filtr a řazení běží v paměti – desítky tisíc poznámek jednoho uživatele (NFR-7).
        var filtered = all.Where(note => tagId is null || note.TagIds.Contains(tagId.Value))
            .OrderByDescending(note => note.IsPinned)
            .ThenByDescending(note => note.UpdatedAtUtc)
            .ThenBy(note => note.Id)
            .ToList();
        return new LocalPage<LocalNote>([.. filtered.Skip(skip).Take(take).Select(ToLocal)], filtered.Count);
    }

    public async Task<LocalPage<LocalNote>> SearchNotesAsync(string? query, NoteState? state, int skip, int take, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var found = await new SqliteSearchIndexAdapter(db).SearchAsync(
            new SearchRequest(SearchQuery.Parse(query), SearchEntityTypes.Note, state?.ToString().ToLowerInvariant(), skip, take),
            cancellationToken);
        var ids = found.EntityIds.ToList();
        var notes = await db.Notes.AsNoTracking().Where(note => ids.Contains(note.Id)).ToDictionaryAsync(note => note.Id, cancellationToken);
        return new LocalPage<LocalNote>([.. ids.Where(notes.ContainsKey).Select(id => ToLocal(notes[id]))], found.TotalCount);
    }

    private async Task<StoreResult<LocalNote>> UpdateNoteAsync(
        Guid id,
        Func<CrumaLocalDbContext, Note, ContentDocument, Task<(Note? Metadata, ContentDocument? Document, string? Error)>> change,
        CancellationToken cancellationToken)
    {
        await using var db = database.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entity = await db.Notes.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return StoreResult<LocalNote>.Failure(LocalErrorCodes.NotFound, "Poznámka neexistuje.");
        }

        (Note? Metadata, ContentDocument? Document, string? Error) result;
        try
        {
            result = await change(db, ToNote(entity), ContentDocument.Parse(entity.Document));
        }
        catch (NotesRuleException exception)
        {
            return StoreResult<LocalNote>.Failure(exception.Code, exception.Message);
        }

        if (result.Error is not null)
        {
            return StoreResult<LocalNote>.Failure(result.Error, result.Error);
        }

        var now = time.GetUtcNow();
        Apply(entity, result.Metadata!, result.Document!, now);
        await RecordLocalVersionAsync(db, entity, now, cancellationToken);
        await EnqueueAsync(db, SyncEntityType.Note, id, SyncOperation.Upsert, now, cancellationToken);
        await IndexAsync(db, entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StoreResult<LocalNote>.Success(ToLocal(entity));
    }

    /// <summary>Lokální verze za editační relaci: úpravy do intervalu nečinnosti aktualizují poslední lokální verzi (VER-006, N-3).</summary>
    private async Task RecordLocalVersionAsync(CrumaLocalDbContext db, LocalNoteEntity note, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var sessionStart = now - options.EditSessionInterval;
        var last = await db.LocalVersions
            .Where(version => version.NoteId == note.Id)
            .OrderByDescending(version => version.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var metadata = JsonSerializer.Serialize(new { note.Title, note.CategoryId, note.TagIds, note.Color, note.IsPinned, note.State }, Json);
        if (last is not null && last.UpdatedAtUtc >= sessionStart)
        {
            last.Document = note.Document;
            last.Metadata = metadata;
            last.UpdatedAtUtc = now;
            return;
        }

        db.LocalVersions.Add(new LocalVersionEntity { NoteId = note.Id, CreatedAtUtc = now, UpdatedAtUtc = now, Document = note.Document, Metadata = metadata });
    }

    /// <summary>
    /// Zapíše čekající změnu. Neodeslaná změna téže entity se jen aktualizuje (offline práce = jedna změna, FR-6 akc. 4);
    /// už odeslaná, ale nepotvrzená změna se nemění a vznikne nová (SYN-003).
    /// </summary>
    internal static async Task EnqueueAsync(CrumaLocalDbContext db, SyncEntityType entityType, Guid entityId, SyncOperation operation, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var typeName = entityType.ToString();
        var unsent = await db.PendingChanges
            .Where(change => change.EntityType == typeName && change.EntityId == entityId && change.SentAtUtc == null)
            .OrderByDescending(change => change.Sequence)
            .FirstOrDefaultAsync(cancellationToken);

        if (unsent is not null)
        {
            unsent.Operation = operation == SyncOperation.Delete ? operation.ToString() : unsent.Operation;
            unsent.ChangedAtUtc = now;
            unsent.LastErrorCode = null;
            return;
        }

        db.PendingChanges.Add(new PendingChangeEntity
        {
            ChangeId = Guid.CreateVersion7(now),
            EntityType = typeName,
            EntityId = entityId,
            Operation = operation.ToString(),
            ChangedAtUtc = now,
        });
    }

    internal static Task IndexAsync(CrumaLocalDbContext db, LocalNoteEntity note, CancellationToken cancellationToken) =>
        new SqliteSearchIndexAdapter(db).UpsertAsync(
            SearchIndexEntry.FromText(SearchEntityTypes.Note, note.Id, note.State.ToLowerInvariant(), PlainTextExtractor.Extract(note.Title, ContentDocument.Parse(note.Document))),
            cancellationToken);

    internal static void Apply(LocalNoteEntity entity, Note metadata, ContentDocument document, DateTimeOffset now)
    {
        entity.Title = metadata.Title;
        entity.CategoryId = metadata.CategoryId;
        entity.TagIds = [.. metadata.TagIds];
        entity.Color = metadata.Color;
        entity.IsPinned = metadata.IsPinned;
        entity.State = metadata.State.ToString();
        entity.Document = document.ToJsonString();
        entity.HasConflict = document.Blocks.Any(block => block.Type == ContentTypes.Conflict);
        entity.UpdatedAtUtc = now;
    }

    internal static Note ToNote(LocalNoteEntity entity) =>
        Note.Restore(entity.Id, entity.Title, entity.CategoryId, entity.TagIds, entity.Color, entity.IsPinned, Enum.Parse<NoteState>(entity.State));

    internal static LocalNote ToLocal(LocalNoteEntity entity) =>
        new(entity.Id, entity.ServerVersion, ToNote(entity), ContentDocument.Parse(entity.Document), entity.HasConflict, entity.UpdatedAtUtc);
}
