using Cruma.Content;
using Cruma.Desktop.Storage;
using Cruma.Notes;
using Cruma.Ui.Services;

namespace Cruma.Desktop.Data;

/// <summary>
/// Datové služby Cruma.Ui nad lokálním úložištěm desktopu (UI-002): čtou a zapisují lokálně, fungují bez sítě
/// (FR-26 akc. 1) a po zápisu jen požádají o synchronizaci (§5.2).
/// </summary>
public abstract class DesktopDataService(DesktopRuntime runtime)
{
    protected DesktopRuntime Runtime => runtime;

    protected async Task<DataResult<TOut>> WithStoreAsync<TIn, TOut>(Func<LocalNotesStore, Task<StoreResult<TIn>>> operation, Func<TIn, TOut> map, bool write)
    {
        if (runtime.Store is not { } store)
        {
            return DataResult<TOut>.Failure(new DataError("unauthenticated", null, "Přihlaste se."));
        }

        var result = await operation(store);
        if (write && result.IsSuccess)
        {
            runtime.Engine?.RequestSync();
        }

        return result.IsSuccess
            ? DataResult<TOut>.Success(map(result.Value))
            : DataResult<TOut>.Failure(new DataError("validation_failed", result.ErrorCode, result.Message ?? string.Empty));
    }

    protected async Task<DataResult<T>> ReadAsync<T>(Func<LocalNotesStore, Task<T>> query)
    {
        if (runtime.Store is not { } store)
        {
            return DataResult<T>.Failure(new DataError("unauthenticated", null, "Přihlaste se."));
        }

        return DataResult<T>.Success(await query(store));
    }

    protected static NoteItem ToItem(LocalNote note) =>
        new(note.Id, note.ServerVersion ?? 0, note.Metadata, note.Document, note.HasConflict, note.UpdatedAtUtc);

    protected static NoteSaveResult Saved(LocalNote note) => new(ToItem(note), "applied", [], []);
}

public sealed class DesktopNoteData(DesktopRuntime runtime) : DesktopDataService(runtime), INoteData
{
    public Task<DataResult<PageResult<NoteItem>>> ListAsync(NoteState state, Guid? categoryId, Guid? tagId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        ReadAsync(async store =>
        {
            var found = await store.ListNotesAsync(state, categoryId, tagId, (page - 1) * pageSize, pageSize, cancellationToken);
            return new PageResult<NoteItem>([.. found.Items.Select(ToItem)], found.TotalCount);
        });

    public Task<DataResult<NoteItem>> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithStoreAsync(store => store.GetNoteAsync(id, cancellationToken), ToItem, write: false);

    public Task<DataResult<NoteCreateResult>> CreateAsync(Guid id, ContentDocument document, CancellationToken cancellationToken = default) =>
        WithStoreAsync(store => store.CreateNoteAsync(id, document, cancellationToken), note => new NoteCreateResult(ToItem(note), Queued: false), write: true);

    public Task<DataResult<NoteSaveResult>> SaveAsync(NoteItem basedOn, NoteChanges changes, CancellationToken cancellationToken = default) =>
        WithStoreAsync(
            store => store.SaveNoteAsync(basedOn.Id, new LocalNoteChanges(changes.Title, changes.CategoryId, changes.TagIds, changes.Color, changes.IsPinned, changes.Document), cancellationToken),
            Saved,
            write: true);

    public Task<DataResult<NoteSaveResult>> ChangeStateAsync(Guid id, NoteStateChange change, CancellationToken cancellationToken = default) =>
        WithStoreAsync(store => store.ChangeStateAsync(id, change switch
        {
            NoteStateChange.Archive => LocalStateChange.Archive,
            NoteStateChange.Unarchive => LocalStateChange.Unarchive,
            NoteStateChange.Trash => LocalStateChange.Trash,
            _ => LocalStateChange.Restore,
        }, cancellationToken), Saved, write: true);

    public async Task<DataResult> DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await WithStoreAsync(store => store.DeleteNotePermanentlyAsync(id, cancellationToken), deleted => deleted, write: true);
        return result.IsSuccess ? DataResult.Success() : DataResult.Failure(result.Error!);
    }

    public Task<DataResult<NoteSaveResult>> ResolveConflictAsync(Guid id, long baseVersion, string blockId, ConflictSide? choice, ContentBlock? editedBlock, CancellationToken cancellationToken = default) =>
        WithStoreAsync(store => store.ResolveConflictAsync(id, blockId, choice, editedBlock, cancellationToken), Saved, write: true);
}

public sealed class DesktopCatalogData(DesktopRuntime runtime) : DesktopDataService(runtime), ICatalogData
{
    public Task<DataResult<IReadOnlyList<CategoryItem>>> ListCategoriesAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<CategoryItem>>(async store => [.. (await store.ListCategoriesAsync(cancellationToken)).Select(category => new CategoryItem(category.Id, category.Name, category.IsDefault))]);

    public Task<DataResult<CategoryItem>> CreateCategoryAsync(string name, CancellationToken cancellationToken = default) =>
        WithStoreAsync(store => store.CreateCategoryAsync(Guid.CreateVersion7(), name, cancellationToken), category => new CategoryItem(category.Id, category.Name, category.IsDefault), write: true);

    public Task<DataResult<CategoryItem>> RenameCategoryAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
        WithStoreAsync(store => store.RenameCategoryAsync(id, name, cancellationToken), category => new CategoryItem(category.Id, category.Name, category.IsDefault), write: true);

    public async Task<DataResult> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await WithStoreAsync(store => store.DeleteCategoryAsync(id, cancellationToken), deleted => deleted, write: true);
        return result.IsSuccess ? DataResult.Success() : DataResult.Failure(result.Error!);
    }

    public Task<DataResult<IReadOnlyList<TagItem>>> ListTagsAsync(CancellationToken cancellationToken = default) =>
        ReadAsync<IReadOnlyList<TagItem>>(async store => [.. (await store.ListTagsAsync(cancellationToken)).Select(tag => new TagItem(tag.Id, tag.Name))]);

    public Task<DataResult<TagItem>> CreateTagAsync(string name, CancellationToken cancellationToken = default) =>
        WithStoreAsync(store => store.CreateTagAsync(Guid.CreateVersion7(), name, cancellationToken), tag => new TagItem(tag.Id, tag.Name), write: true);

    public Task<DataResult<TagItem>> RenameTagAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
        WithStoreAsync(store => store.RenameTagAsync(id, name, cancellationToken), tag => new TagItem(tag.Id, tag.Name), write: true);

    public async Task<DataResult> DeleteTagAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await WithStoreAsync(store => store.DeleteTagAsync(id, cancellationToken), deleted => deleted, write: true);
        return result.IsSuccess ? DataResult.Success() : DataResult.Failure(result.Error!);
    }
}

public sealed class DesktopSearchData(DesktopRuntime runtime) : DesktopDataService(runtime), ISearchData
{
    public Task<DataResult<PageResult<NoteItem>>> SearchAsync(string query, NoteState? state, int page, int pageSize, CancellationToken cancellationToken = default) =>
        ReadAsync(async store =>
        {
            var found = await store.SearchNotesAsync(query, state, (page - 1) * pageSize, pageSize, cancellationToken);
            return new PageResult<NoteItem>([.. found.Items.Select(ToItem)], found.TotalCount);
        });
}

/// <summary>Přihlášení desktopu: relace se pamatuje lokálně, token je chráněný DPAPI (SEC-003).</summary>
public sealed class DesktopSessionData(DesktopRuntime runtime, DesktopOptions options) : ISessionData
{
    public Task<DataResult<SessionInfo?>> GetSessionAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(DataResult<SessionInfo?>.Success(runtime.UserId is { } userId ? new SessionInfo(userId) : null));

    // Přihlášení systémovým prohlížečem (SEC-002) čeká na T-20; do té doby jen vývojové přihlášení v Debug buildu.
    public Task<DataResult<SignInOptions>> GetSignInOptionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(DataResult<SignInOptions>.Success(new SignInOptions([], options.DevelopmentSignIn)));

    public string ExternalSignInUrl(string provider, string returnUrl) => string.Empty;

    public async Task<DataResult<SessionInfo?>> SignInDevelopmentAsync(string subject, CancellationToken cancellationToken = default)
    {
        var userId = await runtime.SignInDevelopmentAsync(subject);
        return userId is { } id
            ? DataResult<SessionInfo?>.Success(new SessionInfo(id))
            : DataResult<SessionInfo?>.Failure(new DataError(DataError.Offline, null, "Přihlášení se nezdařilo – první přihlášení vyžaduje připojení k serveru."));
    }

    public async Task<DataResult> SignOutAsync(CancellationToken cancellationToken = default)
    {
        await runtime.SignOutAsync();
        return DataResult.Success();
    }
}
