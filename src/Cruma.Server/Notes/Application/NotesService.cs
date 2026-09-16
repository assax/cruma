using Cruma.Content;
using Cruma.Notes;
using Cruma.Server.Audit;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes.Persistence;
using Cruma.Server.Search;
using Cruma.Server.Sync;
using Cruma.Sync;
using Cruma.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cruma.Server.Notes.Application;

/// <summary>
/// Jednotná zápisová cesta poznámek (plan.md N-1): ověří vlastnictví (globální filtr), provede merge proti základní
/// verzi přes Cruma.Versioning (VER-002), uloží verzi, aktualizuje index, zapíše změnový feed a audit – vše v jedné
/// transakci. Doménová pravidla jsou ve sdílených knihovnách; služba je jen orchestruje.
/// </summary>
internal sealed partial class NotesService(
    CrumaDbContext db,
    RequestContext requestContext,
    IAuditService audit,
    ISearchService search,
    IChangeFeed changeFeed,
    TimeProvider timeProvider,
    IOptions<NotesOptions> options) : INotesService
{
    private const string DefaultCategoryName = "Poznámky";

    public Task<ServiceResult<NoteWriteResult>> CreateNoteAsync(CreateNoteCommand command, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult<NoteWriteResult>>(async () =>
        {
            var existing = await db.Set<NoteEntity>().SingleOrDefaultAsync(note => note.Id == command.NoteId, cancellationToken);
            if (existing is not null)
            {
                // Opakované vytvoření téže poznámky (fronta zápisů, opakovaná synchronizace) nic nezdvojí.
                return new NoteWriteResult(NoteMapping.ToView(existing), NoteMergeOutcome.Applied, [], []);
            }

            if (await db.AcrossAllUsers<NoteEntity>("kontrola kolize identifikátoru poznámky od klienta").AnyAsync(note => note.Id == command.NoteId, cancellationToken))
            {
                return ServiceError.Validation("Identifikátor poznámky nelze použít.", "note_id_unavailable");
            }

            var defaultCategory = await db.Set<CategoryEntity>().SingleOrDefaultAsync(category => category.IsDefault, cancellationToken);
            if (defaultCategory is null)
            {
                return ServiceError.Validation("Uživatel nemá výchozí kategorii.", "default_category_missing");
            }

            var metadataResult = await BuildMetadataAsync(
                Note.Create(command.NoteId, Category.Restore(defaultCategory.Id, defaultCategory.Name, isDefault: true)),
                command.Metadata,
                cancellationToken);
            if (!metadataResult.IsSuccess)
            {
                return metadataResult.Error!;
            }

            var now = timeProvider.GetUtcNow();
            var version = NoteVersion.First(metadataResult.Value, command.Document, NoteMapping.ToVersionSource(requestContext.ClientType), now);
            var entity = new NoteEntity { Id = command.NoteId, CreatedAtUtc = now };
            NoteMapping.Apply(entity, version.Metadata, version.Document, version.Number, now);
            db.Set<NoteEntity>().Add(entity);
            db.Set<NoteVersionEntity>().Add(NoteMapping.ToEntity(version, [], requestContext.ClientInstanceId));

            await AfterNoteWriteAsync(entity, AuditOperations.NoteCreated, NoteMergeOutcome.Applied, cancellationToken);
            return new NoteWriteResult(NoteMapping.ToView(entity), NoteMergeOutcome.Applied, [], []);
        }, cancellationToken);

    public Task<ServiceResult<NoteWriteResult>> SaveNoteAsync(SaveNoteCommand command, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult<NoteWriteResult>>(async () =>
        {
            var note = await LockNoteAsync(command.NoteId, cancellationToken);
            if (note is null)
            {
                return ServiceError.NotFound();
            }

            var current = await LoadVersionAsync(note.Id, note.CurrentVersion, cancellationToken);
            var baseVersion = command.BaseVersion == current.Number
                ? current
                : command.BaseVersion is > 0 && command.BaseVersion < current.Number
                    ? await LoadVersionAsync(note.Id, command.BaseVersion, cancellationToken)
                    : null;
            if (baseVersion is null)
            {
                return ServiceError.Validation("Základní verze neexistuje.", "base_version_invalid");
            }

            var metadataResult = await BuildMetadataAsync(NoteMapping.ToNote(note), command.Metadata, cancellationToken);
            if (!metadataResult.IsSuccess)
            {
                return metadataResult.Error!;
            }

            var change = Incoming(command.Document, metadataResult.Value, command.ChangedAtUtc);
            return await StoreAsync(note, current, baseVersion, change, AuditOperations.NoteUpdated, allowCoalescing: true, cancellationToken);
        }, cancellationToken);

    public Task<ServiceResult<NoteWriteResult>> ChangeNoteStateAsync(Guid noteId, NoteStateChange change, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult<NoteWriteResult>>(async () =>
        {
            var note = await LockNoteAsync(noteId, cancellationToken);
            if (note is null)
            {
                return ServiceError.NotFound();
            }

            var current = await LoadVersionAsync(note.Id, note.CurrentVersion, cancellationToken);
            Note changed;
            try
            {
                changed = change switch
                {
                    NoteStateChange.Archive => current.Metadata.Archive(),
                    NoteStateChange.Unarchive => current.Metadata.Unarchive(),
                    NoteStateChange.Trash => current.Metadata.MoveToTrash(),
                    _ => current.Metadata.RestoreFromTrash(),
                };
            }
            catch (NotesRuleException exception)
            {
                return ServiceError.Validation(exception.Message, exception.Code);
            }

            if (changed.Equals(current.Metadata))
            {
                return new NoteWriteResult(NoteMapping.ToView(note), NoteMergeOutcome.Applied, [], []);
            }

            var operation = change switch
            {
                NoteStateChange.Archive => AuditOperations.NoteArchived,
                NoteStateChange.Trash => AuditOperations.NoteTrashed,
                _ => AuditOperations.NoteRestored,
            };
            return await StoreAsync(note, current, current, Incoming(current.Document, changed, null), operation, allowCoalescing: false, cancellationToken);
        }, cancellationToken);

    public Task<ServiceResult<NoteWriteResult>> ResolveConflictAsync(ResolveConflictCommand command, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult<NoteWriteResult>>(async () =>
        {
            var note = await LockNoteAsync(command.NoteId, cancellationToken);
            if (note is null)
            {
                return ServiceError.NotFound();
            }

            var current = await LoadVersionAsync(note.Id, note.CurrentVersion, cancellationToken);
            var baseVersion = command.BaseVersion == current.Number
                ? current
                : command.BaseVersion is > 0 && command.BaseVersion < current.Number
                    ? await LoadVersionAsync(note.Id, command.BaseVersion, cancellationToken)
                    : null;
            if (baseVersion is null)
            {
                return ServiceError.Validation("Základní verze neexistuje.", "base_version_invalid");
            }

            // Vyřešení se provede nad verzí, kterou klient viděl, a sloučí se jako běžná úprava –
            // mezitím vzniklé změny jiných bloků zůstanou (I1-D-1).
            ContentDocument resolved;
            try
            {
                resolved = command switch
                {
                    { Choice: { } side } => ConflictResolution.ChooseVariant(baseVersion.Document, command.BlockId, side),
                    { EditedBlock: { } edited } => ConflictResolution.ReplaceWithEdit(baseVersion.Document, command.BlockId, edited),
                    _ => throw new ArgumentException("Chybí vybraná varianta nebo upravený blok."),
                };
            }
            catch (ArgumentException exception)
            {
                return ServiceError.Validation(exception.Message, "conflict_resolution_invalid");
            }

            var change = Incoming(resolved, baseVersion.Metadata, null);
            return await StoreAsync(note, current, baseVersion, change, AuditOperations.NoteConflictResolved, allowCoalescing: false, cancellationToken);
        }, cancellationToken);

    public Task<ServiceResult> DeleteNotePermanentlyAsync(Guid noteId, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult>(async () =>
        {
            var note = await LockNoteAsync(noteId, cancellationToken);
            if (note is null)
            {
                return ServiceError.NotFound();
            }

            try
            {
                NoteMapping.ToNote(note).EnsureCanBeDeletedPermanently();
            }
            catch (NotesRuleException exception)
            {
                return ServiceError.Validation(exception.Message, exception.Code);
            }

            // Trvalé smazání odstraní i verze (VER-008, FR-5 akc. 3).
            await db.Set<NoteVersionEntity>().Where(version => version.NoteId == noteId).ExecuteDeleteAsync(cancellationToken);
            db.Set<NoteEntity>().Remove(note);
            await search.RemoveNoteAsync(noteId, cancellationToken);
            await changeFeed.AppendAsync(SyncEntityType.Note, noteId, null, deleted: true, cancellationToken);
            audit.Record(new AuditEvent(AuditOperations.NoteDeletedPermanently, AuditObjectTypes.Note, noteId.ToString()));
            return ServiceResult.Success();
        }, cancellationToken);

    public async Task<ServiceResult<NoteView>> GetNoteAsync(Guid noteId, CancellationToken cancellationToken)
    {
        var note = await db.Set<NoteEntity>().AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == noteId, cancellationToken);
        return note is null ? ServiceError.NotFound() : NoteMapping.ToView(note);
    }

    public async Task<PagedList<NoteView>> ListNotesAsync(NoteListQuery query, CancellationToken cancellationToken)
    {
        var state = query.State.ToString();
        var notes = db.Set<NoteEntity>().AsNoTracking().Where(note => note.State == state);
        if (query.CategoryId is { } categoryId)
        {
            notes = notes.Where(note => note.CategoryId == categoryId);
        }

        if (query.TagId is { } tagId)
        {
            notes = notes.Where(note => note.TagIds.Contains(tagId));
        }

        var total = await notes.CountAsync(cancellationToken);
        var page = await notes
            .OrderByDescending(note => note.IsPinned).ThenByDescending(note => note.UpdatedAtUtc).ThenBy(note => note.Id)
            .Skip(query.Skip).Take(query.Take)
            .ToListAsync(cancellationToken);
        return new PagedList<NoteView>(page.Select(NoteMapping.ToView).ToList(), total);
    }

    public async Task<IReadOnlyList<NoteView>> GetNotesAsync(IReadOnlyList<Guid> noteIds, CancellationToken cancellationToken)
    {
        var notes = await db.Set<NoteEntity>().AsNoTracking().Where(note => noteIds.Contains(note.Id)).ToDictionaryAsync(note => note.Id, cancellationToken);
        return noteIds.Where(notes.ContainsKey).Select(id => NoteMapping.ToView(notes[id])).ToList();
    }

    public async Task<PagedList<NoteView>> SearchNotesAsync(string? query, NoteState? state, int skip, int take, CancellationToken cancellationToken)
    {
        var found = await search.SearchNotesAsync(query, state, skip, take, cancellationToken);
        return new PagedList<NoteView>(await GetNotesAsync(found.EntityIds, cancellationToken), found.TotalCount);
    }

    /// <summary>Uloží výsledek merge: nová verze nebo spojení s poslední verzí (N-3), stav poznámky, index, feed, audit.</summary>
    private async Task<ServiceResult<NoteWriteResult>> StoreAsync(
        NoteEntity note,
        NoteVersion current,
        NoteVersion baseVersion,
        IncomingNoteChange change,
        string auditOperation,
        bool allowCoalescing,
        CancellationToken cancellationToken)
    {
        var merge = NoteMerge.Merge(baseVersion, current, change);
        var now = timeProvider.GetUtcNow();
        var currentEntity = await db.Set<NoteVersionEntity>()
            .SingleAsync(version => version.NoteId == note.Id && version.Number == current.Number, cancellationToken);

        long versionNumber;
        if (allowCoalescing && CanCoalesce(currentEntity, merge, change, now))
        {
            // Spojení uložení do poslední verze (plan.md N-3, FR-6 akc. 3).
            currentEntity.Document = merge.Document.ToJsonString();
            currentEntity.Metadata = NoteMapping.SerializeMetadata(merge.Metadata);
            currentEntity.UpdatedAtUtc = now;
            versionNumber = currentEntity.Number;
        }
        else
        {
            var version = merge.ToVersion(current, change, baseVersion.Number, now);
            db.Set<NoteVersionEntity>().Add(NoteMapping.ToEntity(version, merge.Notices, requestContext.ClientInstanceId));
            versionNumber = version.Number;
        }

        // Úprava, která zároveň změnila stav (typicky ze synchronizace), se audituje jako změna stavu (FR-35 akc. 1).
        if (auditOperation == AuditOperations.NoteUpdated && merge.Metadata.State != current.Metadata.State)
        {
            auditOperation = merge.Metadata.State switch
            {
                NoteState.Archived => AuditOperations.NoteArchived,
                NoteState.Trashed => AuditOperations.NoteTrashed,
                _ => AuditOperations.NoteRestored,
            };
        }

        NoteMapping.Apply(note, merge.Metadata, merge.Document, versionNumber, now);
        await AfterNoteWriteAsync(note, auditOperation, merge.Outcome, cancellationToken);
        return new NoteWriteResult(NoteMapping.ToView(note), merge.Outcome, merge.OverwrittenValues, merge.Notices);
    }

    private bool CanCoalesce(NoteVersionEntity currentEntity, NoteMergeResult merge, IncomingNoteChange change, DateTimeOffset now) =>
        merge.Outcome == NoteMergeOutcome.Applied
        && requestContext.ClientInstanceId is { } instance
        && currentEntity.ClientInstanceId == instance
        && currentEntity.Source == change.Source.ToString()
        && !currentEntity.HasConflict
        && now - currentEntity.UpdatedAtUtc <= options.Value.VersionCoalescingInterval;

    private async Task AfterNoteWriteAsync(NoteEntity note, string auditOperation, NoteMergeOutcome outcome, CancellationToken cancellationToken)
    {
        var metadata = NoteMapping.ToNote(note);
        await search.IndexNoteAsync(note.Id, metadata.Title, ContentDocument.Parse(note.Document), metadata.State, cancellationToken);
        await changeFeed.AppendAsync(SyncEntityType.Note, note.Id, note.CurrentVersion, deleted: false, cancellationToken);
        audit.Record(new AuditEvent(
            auditOperation,
            AuditObjectTypes.Note,
            note.Id.ToString(),
            Attributes: new Dictionary<string, string>
            {
                ["version"] = note.CurrentVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["outcome"] = outcome.ToString().ToLowerInvariant(),
            }));
    }

    private IncomingNoteChange Incoming(ContentDocument document, Note metadata, DateTimeOffset? changedAtUtc) =>
        new(document, metadata, NoteMapping.ToVersionSource(requestContext.ClientType), changedAtUtc ?? timeProvider.GetUtcNow());

    /// <summary>Načte poznámku aktuálního uživatele se zámkem řádku do konce transakce – zápisy téže poznámky jdou za sebou.</summary>
    private Task<NoteEntity?> LockNoteAsync(Guid noteId, CancellationToken cancellationToken) =>
        db.Set<NoteEntity>()
            .FromSql($"SELECT * FROM notes.notes WHERE id = {noteId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<NoteVersion> LoadVersionAsync(Guid noteId, long number, CancellationToken cancellationToken) =>
        NoteMapping.ToVersion(await db.Set<NoteVersionEntity>().AsNoTracking()
            .SingleAsync(version => version.NoteId == noteId && version.Number == number, cancellationToken));

    /// <summary>Vlastnosti od klienta nad výchozím stavem; kategorie a štítky musí patřit uživateli.</summary>
    private async Task<ServiceResult<Note>> BuildMetadataAsync(Note start, NoteMetadataInput? input, CancellationToken cancellationToken)
    {
        if (input is null)
        {
            return start;
        }

        if (!await db.Set<CategoryEntity>().AnyAsync(category => category.Id == input.CategoryId, cancellationToken))
        {
            return ServiceError.Validation("Kategorie neexistuje.", "category_not_found");
        }

        var tagIds = input.TagIds.Distinct().ToList();
        if (await db.Set<TagEntity>().CountAsync(tag => tagIds.Contains(tag.Id), cancellationToken) != tagIds.Count)
        {
            return ServiceError.Validation("Štítek neexistuje.", "tag_not_found");
        }

        try
        {
            return Note.Restore(start.Id, input.Title, input.CategoryId, tagIds, input.Color, input.IsPinned, input.State ?? start.State);
        }
        catch (NotesRuleException exception)
        {
            return ServiceError.Validation(exception.Message, exception.Code);
        }
    }
}
