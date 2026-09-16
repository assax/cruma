using Cruma.Desktop.Storage.Persistence;
using Cruma.Notes;
using Cruma.Sync;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Desktop.Storage;

/// <summary>Kategorie a štítky v lokálním úložišti (FR-3, FR-4).</summary>
public sealed partial class LocalNotesStore
{
    public async Task<IReadOnlyList<LocalCategory>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var categories = await db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        return [.. categories.OrderByDescending(category => category.IsDefault).ThenBy(category => category.Name).Select(category => new LocalCategory(category.Id, category.Name, category.IsDefault))];
    }

    public async Task<StoreResult<LocalCategory>> CreateCategoryAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        Category category;
        try
        {
            category = Category.Create(id, name);
        }
        catch (NotesRuleException exception)
        {
            return StoreResult<LocalCategory>.Failure(exception.Code, exception.Message);
        }

        await using var db = database.CreateContext();
        db.Categories.Add(new LocalCategoryEntity { Id = category.Id, Name = category.Name });
        await EnqueueAsync(db, SyncEntityType.Category, id, SyncOperation.Upsert, time.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return StoreResult<LocalCategory>.Success(new LocalCategory(category.Id, category.Name, false));
    }

    public async Task<StoreResult<LocalCategory>> RenameCategoryAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var entity = await db.Categories.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return StoreResult<LocalCategory>.Failure(LocalErrorCodes.NotFound, "Kategorie neexistuje.");
        }

        try
        {
            entity.Name = Category.Restore(entity.Id, entity.Name, entity.IsDefault).Rename(name).Name;
        }
        catch (NotesRuleException exception)
        {
            return StoreResult<LocalCategory>.Failure(exception.Code, exception.Message);
        }

        await EnqueueAsync(db, SyncEntityType.Category, id, SyncOperation.Upsert, time.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return StoreResult<LocalCategory>.Success(new LocalCategory(entity.Id, entity.Name, entity.IsDefault));
    }

    /// <summary>
    /// Smaže kategorii a přesune její poznámky do výchozí (FR-3 akc. 3). Přesun zopakuje server při zpracování smazání,
    /// proto poznámky nedostanou vlastní čekající změnu.
    /// </summary>
    public async Task<StoreResult<bool>> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entity = await db.Categories.FindAsync([id], cancellationToken);
        var defaultEntity = await db.Categories.SingleOrDefaultAsync(category => category.IsDefault, cancellationToken);
        if (entity is null || defaultEntity is null)
        {
            return StoreResult<bool>.Failure(LocalErrorCodes.NotFound, "Kategorie neexistuje.");
        }

        var notes = await db.Notes.Where(note => note.CategoryId == id).ToListAsync(cancellationToken);
        IReadOnlyList<Note> moved;
        try
        {
            moved = Category.Restore(entity.Id, entity.Name, entity.IsDefault)
                .Delete(Category.Restore(defaultEntity.Id, defaultEntity.Name, isDefault: true), notes.Select(ToNote));
        }
        catch (NotesRuleException exception)
        {
            return StoreResult<bool>.Failure(exception.Code, exception.Message);
        }

        foreach (var note in notes)
        {
            note.CategoryId = moved.Single(movedNote => movedNote.Id == note.Id).CategoryId;
        }

        db.Categories.Remove(entity);
        await EnqueueAsync(db, SyncEntityType.Category, id, SyncOperation.Delete, time.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StoreResult<bool>.Success(true);
    }

    public async Task<IReadOnlyList<LocalTag>> ListTagsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var tags = await db.Tags.AsNoTracking().ToListAsync(cancellationToken);
        return [.. tags.OrderBy(tag => tag.Name).Select(tag => new LocalTag(tag.Id, tag.Name))];
    }

    public async Task<StoreResult<LocalTag>> CreateTagAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        Tag tag;
        try
        {
            tag = Tag.Create(id, name);
        }
        catch (NotesRuleException exception)
        {
            return StoreResult<LocalTag>.Failure(exception.Code, exception.Message);
        }

        await using var db = database.CreateContext();
        db.Tags.Add(new LocalTagEntity { Id = tag.Id, Name = tag.Name });
        await EnqueueAsync(db, SyncEntityType.Tag, id, SyncOperation.Upsert, time.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return StoreResult<LocalTag>.Success(new LocalTag(tag.Id, tag.Name));
    }

    public async Task<StoreResult<LocalTag>> RenameTagAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        var entity = await db.Tags.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return StoreResult<LocalTag>.Failure(LocalErrorCodes.NotFound, "Štítek neexistuje.");
        }

        try
        {
            entity.Name = Tag.Create(entity.Id, entity.Name).Rename(name).Name;
        }
        catch (NotesRuleException exception)
        {
            return StoreResult<LocalTag>.Failure(exception.Code, exception.Message);
        }

        await EnqueueAsync(db, SyncEntityType.Tag, id, SyncOperation.Upsert, time.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return StoreResult<LocalTag>.Success(new LocalTag(entity.Id, entity.Name));
    }

    /// <summary>Smaže štítek a odebere ho z poznámek; poznámky zůstanou (FR-4 akc. 3). Server odebrání zopakuje.</summary>
    public async Task<StoreResult<bool>> DeleteTagAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = database.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entity = await db.Tags.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return StoreResult<bool>.Failure(LocalErrorCodes.NotFound, "Štítek neexistuje.");
        }

        var notes = await db.Notes.ToListAsync(cancellationToken);
        var changed = Tag.Create(entity.Id, entity.Name).Delete(notes.Select(ToNote));
        foreach (var note in changed)
        {
            notes.Single(entityNote => entityNote.Id == note.Id).TagIds = [.. note.TagIds];
        }

        db.Tags.Remove(entity);
        await EnqueueAsync(db, SyncEntityType.Tag, id, SyncOperation.Delete, time.GetUtcNow(), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StoreResult<bool>.Success(true);
    }
}
