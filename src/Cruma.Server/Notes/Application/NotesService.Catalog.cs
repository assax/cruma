using Cruma.Notes;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes.Persistence;
using Cruma.Sync;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Server.Notes.Application;

/// <summary>Kategorie a štítky. Přesun poznámek při smazání jde přes zápisovou cestu poznámek (N-1), takže vznikne verze.</summary>
internal sealed partial class NotesService
{
    public Task<ServiceResult<CategoryView>> CreateCategoryAsync(Guid categoryId, string name, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult<CategoryView>>(async () =>
        {
            var existing = await db.Set<CategoryEntity>().SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
            if (existing is not null)
            {
                return ToView(existing);
            }

            if (await db.AcrossAllUsers<CategoryEntity>("kontrola kolize identifikátoru kategorie od klienta").AnyAsync(category => category.Id == categoryId, cancellationToken))
            {
                return ServiceError.Validation("Identifikátor kategorie nelze použít.", "category_id_unavailable");
            }

            var category = TryDomain(() => Category.Create(categoryId, name), out var error);
            if (category is null)
            {
                return error!;
            }

            var now = timeProvider.GetUtcNow();
            var entity = new CategoryEntity { Id = category.Id, Name = category.Name, CreatedAtUtc = now, UpdatedAtUtc = now };
            db.Set<CategoryEntity>().Add(entity);
            await changeFeed.AppendAsync(SyncEntityType.Category, entity.Id, null, deleted: false, cancellationToken);
            return ToView(entity);
        }, cancellationToken);

    public Task<ServiceResult<CategoryView>> RenameCategoryAsync(Guid categoryId, string name, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult<CategoryView>>(async () =>
        {
            var entity = await db.Set<CategoryEntity>().SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
            if (entity is null)
            {
                return ServiceError.NotFound();
            }

            var renamed = TryDomain(() => Category.Restore(entity.Id, entity.Name, entity.IsDefault).Rename(name), out var error);
            if (renamed is null)
            {
                return error!;
            }

            entity.Name = renamed.Name;
            entity.UpdatedAtUtc = timeProvider.GetUtcNow();
            await changeFeed.AppendAsync(SyncEntityType.Category, entity.Id, null, deleted: false, cancellationToken);
            return ToView(entity);
        }, cancellationToken);

    public Task<ServiceResult> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult>(async () =>
        {
            var entity = await db.Set<CategoryEntity>().SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
            if (entity is null)
            {
                return ServiceError.NotFound();
            }

            var defaultEntity = await db.Set<CategoryEntity>().SingleAsync(category => category.IsDefault, cancellationToken);
            var noteIds = await db.Set<NoteEntity>().Where(note => note.CategoryId == categoryId).Select(note => note.Id).ToListAsync(cancellationToken);
            var category = Category.Restore(entity.Id, entity.Name, entity.IsDefault);
            var defaultCategory = Category.Restore(defaultEntity.Id, defaultEntity.Name, isDefault: true);
            try
            {
                // Výchozí kategorii smazat nejde (FR-3 akc. 4).
                category.Delete(defaultCategory, []);
            }
            catch (NotesRuleException exception)
            {
                return ServiceError.Validation(exception.Message, exception.Code);
            }

            foreach (var noteId in noteIds)
            {
                var note = await LockNoteAsync(noteId, cancellationToken);
                var current = await LoadVersionAsync(noteId, note!.CurrentVersion, cancellationToken);

                // Poznámky mazané kategorie přejdou do výchozí (FR-3 akc. 3).
                var moved = category.Delete(defaultCategory, [current.Metadata]);
                var result = await StoreAsync(note, current, current, Incoming(current.Document, moved[0], null),
                    Audit.AuditOperations.NoteUpdated, allowCoalescing: false, cancellationToken);
                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            db.Set<CategoryEntity>().Remove(entity);
            await changeFeed.AppendAsync(SyncEntityType.Category, entity.Id, null, deleted: true, cancellationToken);
            return ServiceResult.Success();
        }, cancellationToken);

    public async Task<ServiceResult<CategoryView>> GetCategoryAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var entity = await db.Set<CategoryEntity>().AsNoTracking().SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
        return entity is null ? ServiceError.NotFound() : ToView(entity);
    }

    public async Task<PagedList<CategoryView>> ListCategoriesAsync(int skip, int take, CancellationToken cancellationToken)
    {
        var categories = db.Set<CategoryEntity>().AsNoTracking();
        var total = await categories.CountAsync(cancellationToken);
        var page = await categories.OrderByDescending(category => category.IsDefault).ThenBy(category => category.Name).ThenBy(category => category.Id)
            .Skip(skip).Take(take).ToListAsync(cancellationToken);
        return new PagedList<CategoryView>(page.Select(ToView).ToList(), total);
    }

    public async Task<CategoryView> EnsureDefaultCategoryAsync(CancellationToken cancellationToken)
    {
        var existing = await db.Set<CategoryEntity>().SingleOrDefaultAsync(category => category.IsDefault, cancellationToken);
        if (existing is not null)
        {
            return ToView(existing);
        }

        var now = timeProvider.GetUtcNow();
        var category = Category.CreateDefault(Guid.CreateVersion7(now), DefaultCategoryName);
        var entity = new CategoryEntity { Id = category.Id, Name = category.Name, IsDefault = true, CreatedAtUtc = now, UpdatedAtUtc = now };
        db.Set<CategoryEntity>().Add(entity);
        await changeFeed.AppendAsync(SyncEntityType.Category, entity.Id, null, deleted: false, cancellationToken);
        return ToView(entity);
    }

    public Task<ServiceResult<TagView>> CreateTagAsync(Guid tagId, string name, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult<TagView>>(async () =>
        {
            var existing = await db.Set<TagEntity>().SingleOrDefaultAsync(tag => tag.Id == tagId, cancellationToken);
            if (existing is not null)
            {
                return ToView(existing);
            }

            if (await db.AcrossAllUsers<TagEntity>("kontrola kolize identifikátoru štítku od klienta").AnyAsync(tag => tag.Id == tagId, cancellationToken))
            {
                return ServiceError.Validation("Identifikátor štítku nelze použít.", "tag_id_unavailable");
            }

            var tag = TryDomain(() => Tag.Create(tagId, name), out var error);
            if (tag is null)
            {
                return error!;
            }

            var now = timeProvider.GetUtcNow();
            var entity = new TagEntity { Id = tag.Id, Name = tag.Name, CreatedAtUtc = now, UpdatedAtUtc = now };
            db.Set<TagEntity>().Add(entity);
            await changeFeed.AppendAsync(SyncEntityType.Tag, entity.Id, null, deleted: false, cancellationToken);
            return ToView(entity);
        }, cancellationToken);

    public Task<ServiceResult<TagView>> RenameTagAsync(Guid tagId, string name, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult<TagView>>(async () =>
        {
            var entity = await db.Set<TagEntity>().SingleOrDefaultAsync(tag => tag.Id == tagId, cancellationToken);
            if (entity is null)
            {
                return ServiceError.NotFound();
            }

            var renamed = TryDomain(() => Tag.Create(entity.Id, entity.Name).Rename(name), out var error);
            if (renamed is null)
            {
                return error!;
            }

            entity.Name = renamed.Name;
            entity.UpdatedAtUtc = timeProvider.GetUtcNow();
            await changeFeed.AppendAsync(SyncEntityType.Tag, entity.Id, null, deleted: false, cancellationToken);
            return ToView(entity);
        }, cancellationToken);

    public Task<ServiceResult> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken) =>
        db.InTransactionAsync<ServiceResult>(async () =>
        {
            var entity = await db.Set<TagEntity>().SingleOrDefaultAsync(tag => tag.Id == tagId, cancellationToken);
            if (entity is null)
            {
                return ServiceError.NotFound();
            }

            var tag = Tag.Create(entity.Id, entity.Name);
            var noteIds = await db.Set<NoteEntity>().Where(note => note.TagIds.Contains(tagId)).Select(note => note.Id).ToListAsync(cancellationToken);
            foreach (var noteId in noteIds)
            {
                var note = await LockNoteAsync(noteId, cancellationToken);
                var current = await LoadVersionAsync(noteId, note!.CurrentVersion, cancellationToken);

                // Smazání štítku ho odebere z poznámek, poznámky zůstanou (FR-4 akc. 3).
                var changed = tag.Delete([current.Metadata]);
                if (changed.Count == 0)
                {
                    continue;
                }

                var result = await StoreAsync(note, current, current, Incoming(current.Document, changed[0], null),
                    Audit.AuditOperations.NoteUpdated, allowCoalescing: false, cancellationToken);
                if (!result.IsSuccess)
                {
                    return result;
                }
            }

            db.Set<TagEntity>().Remove(entity);
            await changeFeed.AppendAsync(SyncEntityType.Tag, entity.Id, null, deleted: true, cancellationToken);
            return ServiceResult.Success();
        }, cancellationToken);

    public async Task<ServiceResult<TagView>> GetTagAsync(Guid tagId, CancellationToken cancellationToken)
    {
        var entity = await db.Set<TagEntity>().AsNoTracking().SingleOrDefaultAsync(tag => tag.Id == tagId, cancellationToken);
        return entity is null ? ServiceError.NotFound() : ToView(entity);
    }

    public async Task<PagedList<TagView>> ListTagsAsync(int skip, int take, CancellationToken cancellationToken)
    {
        var tags = db.Set<TagEntity>().AsNoTracking();
        var total = await tags.CountAsync(cancellationToken);
        var page = await tags.OrderBy(tag => tag.Name).ThenBy(tag => tag.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return new PagedList<TagView>(page.Select(ToView).ToList(), total);
    }

    private static CategoryView ToView(CategoryEntity entity) => new(entity.Id, entity.Name, entity.IsDefault);

    private static TagView ToView(TagEntity entity) => new(entity.Id, entity.Name);

    private static T? TryDomain<T>(Func<T> action, out ServiceError? error)
        where T : class
    {
        try
        {
            error = null;
            return action();
        }
        catch (NotesRuleException exception)
        {
            error = ServiceError.Validation(exception.Message, exception.Code);
            return null;
        }
    }
}
