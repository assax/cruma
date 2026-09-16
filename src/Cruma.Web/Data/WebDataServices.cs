using System.Text.Json;
using System.Text.Json.Nodes;
using Cruma.Api.Client;
using Cruma.Api.Contracts;
using Cruma.Content;
using Cruma.Notes;
using Cruma.Ui.Services;
using Cruma.Web.Platform;

namespace Cruma.Web.Data;

/// <summary>Převody mezi kontrakty API a modely UI a hlášení dostupnosti serveru.</summary>
internal static class WebMapping
{
    public static NoteItem ToItem(NoteDto dto) =>
        new(dto.Id, dto.Version, Note.Restore(dto.Id, dto.Title, dto.CategoryId, dto.TagIds, dto.Color, dto.IsPinned, dto.State),
            ContentDocument.Parse(dto.Document.GetRawText()), dto.HasConflict, dto.UpdatedAtUtc);

    public static NoteSaveResult ToSaveResult(NoteWriteResponse response) =>
        new(ToItem(response.Note), response.Outcome,
            [.. response.OverwrittenFields.Select(field => new OverwrittenValueInfo(field.Field, field.OverwrittenValue, field.WinningValue))],
            [.. response.Notices.Select(notice => notice.Kind)]);

    public static JsonElement ToElement(ContentDocument document) => JsonSerializer.SerializeToElement(document.ToJson());

    public static DataError ToError(ApiError error) =>
        error.IsNetworkFailure ? new DataError(DataError.Offline, null, "Server není dostupný.") : new DataError(error.Code, error.Rule, error.Detail ?? string.Empty);
}

/// <summary>Společné zpracování výsledku API: síťová chyba přepne stav připojení na offline.</summary>
public abstract class ApiDataService(BrowserConnectivity connectivity)
{
    protected async Task<DataResult<TOut>> CallAsync<TIn, TOut>(Func<Task<ApiResult<TIn>>> call, Func<TIn, TOut> map)
    {
        var result = await call();
        connectivity.ReportServerReachable(result.Error is not { IsNetworkFailure: true });
        return result.IsSuccess ? DataResult<TOut>.Success(map(result.Value)) : DataResult<TOut>.Failure(WebMapping.ToError(result.Error!));
    }

    protected async Task<DataResult> CallAsync(Func<Task<ApiResult>> call)
    {
        var result = await call();
        connectivity.ReportServerReachable(result.Error is not { IsNetworkFailure: true });
        return result.IsSuccess ? DataResult.Success() : DataResult.Failure(WebMapping.ToError(result.Error!));
    }
}

/// <summary>Poznámky tenkého klienta přes REST API; nová poznámka bez připojení jde do fronty (FR-30).</summary>
public sealed class WebNoteData(CrumaApiClient api, BrowserConnectivity connectivity, WriteQueue queue) : ApiDataService(connectivity), INoteData
{
    private readonly BrowserConnectivity connectivity = connectivity;

    public Task<DataResult<PageResult<NoteItem>>> ListAsync(NoteState state, Guid? categoryId, Guid? tagId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.ListNotesAsync(state, categoryId, tagId, page, pageSize, cancellationToken),
            response => new PageResult<NoteItem>([.. response.Items.Select(WebMapping.ToItem)], response.TotalCount));

    public Task<DataResult<NoteItem>> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.GetNoteAsync(id, cancellationToken), WebMapping.ToItem);

    public async Task<DataResult<NoteCreateResult>> CreateAsync(Guid id, ContentDocument document, CancellationToken cancellationToken = default)
    {
        if (connectivity.IsOnline)
        {
            var created = await CallAsync(() => api.CreateNoteAsync(new CreateNoteRequest(id, WebMapping.ToElement(document)), cancellationToken),
                response => new NoteCreateResult(WebMapping.ToItem(response.Note), Queued: false));
            if (created.Error is not { IsOffline: true })
            {
                return created;
            }
        }

        // Bez připojení se poznámka uloží do fronty a odešle se sama (FR-30 akc. 1, 3).
        await queue.EnqueueAsync(id, document);
        return DataResult<NoteCreateResult>.Success(new NoteCreateResult(null, Queued: true));
    }

    public Task<DataResult<NoteSaveResult>> SaveAsync(NoteItem basedOn, NoteChanges changes, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.UpdateNoteAsync(basedOn.Id,
                new UpdateNoteRequest(basedOn.Version, changes.Title, changes.CategoryId, changes.TagIds, changes.Color, changes.IsPinned, WebMapping.ToElement(changes.Document)),
                cancellationToken),
            WebMapping.ToSaveResult);

    public Task<DataResult<NoteSaveResult>> ChangeStateAsync(Guid id, NoteStateChange change, CancellationToken cancellationToken = default) =>
        CallAsync(() => change switch
        {
            NoteStateChange.Archive => api.ArchiveNoteAsync(id, cancellationToken),
            NoteStateChange.Unarchive => api.UnarchiveNoteAsync(id, cancellationToken),
            NoteStateChange.Trash => api.TrashNoteAsync(id, cancellationToken),
            _ => api.RestoreNoteAsync(id, cancellationToken),
        }, WebMapping.ToSaveResult);

    public Task<DataResult> DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.DeleteNotePermanentlyAsync(id, cancellationToken));

    public Task<DataResult<NoteSaveResult>> ResolveConflictAsync(Guid id, long baseVersion, string blockId, ConflictSide? choice, ContentBlock? editedBlock, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.ResolveConflictAsync(id, blockId,
                new ResolveConflictRequest(baseVersion, choice?.ToString().ToLowerInvariant(), editedBlock is null ? null : JsonSerializer.SerializeToElement<JsonNode>(editedBlock.ToJson())),
                cancellationToken),
            WebMapping.ToSaveResult);
}

public sealed class WebCatalogData(CrumaApiClient api, BrowserConnectivity connectivity) : ApiDataService(connectivity), ICatalogData
{
    public Task<DataResult<IReadOnlyList<CategoryItem>>> ListCategoriesAsync(CancellationToken cancellationToken = default) =>
        CallAsync(() => api.ListCategoriesAsync(cancellationToken: cancellationToken),
            response => (IReadOnlyList<CategoryItem>)[.. response.Items.Select(category => new CategoryItem(category.Id, category.Name, category.IsDefault))]);

    public Task<DataResult<CategoryItem>> CreateCategoryAsync(string name, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.CreateCategoryAsync(new CreateCategoryRequest(Guid.CreateVersion7(), name), cancellationToken),
            category => new CategoryItem(category.Id, category.Name, category.IsDefault));

    public Task<DataResult<CategoryItem>> RenameCategoryAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.RenameCategoryAsync(id, name, cancellationToken), category => new CategoryItem(category.Id, category.Name, category.IsDefault));

    public Task<DataResult> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.DeleteCategoryAsync(id, cancellationToken));

    public Task<DataResult<IReadOnlyList<TagItem>>> ListTagsAsync(CancellationToken cancellationToken = default) =>
        CallAsync(() => api.ListTagsAsync(cancellationToken: cancellationToken),
            response => (IReadOnlyList<TagItem>)[.. response.Items.Select(tag => new TagItem(tag.Id, tag.Name))]);

    public Task<DataResult<TagItem>> CreateTagAsync(string name, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.CreateTagAsync(new CreateTagRequest(Guid.CreateVersion7(), name), cancellationToken), tag => new TagItem(tag.Id, tag.Name));

    public Task<DataResult<TagItem>> RenameTagAsync(Guid id, string name, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.RenameTagAsync(id, name, cancellationToken), tag => new TagItem(tag.Id, tag.Name));

    public Task<DataResult> DeleteTagAsync(Guid id, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.DeleteTagAsync(id, cancellationToken));
}

public sealed class WebSearchData(CrumaApiClient api, BrowserConnectivity connectivity) : ApiDataService(connectivity), ISearchData
{
    public Task<DataResult<PageResult<NoteItem>>> SearchAsync(string query, NoteState? state, int page, int pageSize, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.SearchAsync(query, state, page, pageSize, cancellationToken),
            response => new PageResult<NoteItem>([.. response.Items.Select(WebMapping.ToItem)], response.TotalCount));
}

/// <summary>Cookie session na stejném originu (SEC-004): prohlížeč nikdy nedrží tokeny.</summary>
public sealed class WebSessionData(CrumaApiClient api, BrowserConnectivity connectivity) : ApiDataService(connectivity), ISessionData
{
    public async Task<DataResult<SessionInfo?>> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        var result = await CallAsync(() => api.GetCurrentUserAsync(cancellationToken), user => (SessionInfo?)new SessionInfo(user.UserId));
        return result.Error?.Code == "unauthenticated" ? DataResult<SessionInfo?>.Success(null) : result;
    }

    public Task<DataResult<SignInOptions>> GetSignInOptionsAsync(CancellationToken cancellationToken = default) =>
        CallAsync(() => api.GetSignInProvidersAsync(cancellationToken), providers => new SignInOptions(providers.ExternalProviders, providers.DevelopmentSignIn));

    public string ExternalSignInUrl(string provider, string returnUrl) =>
        $"auth/sign-in/{Uri.EscapeDataString(provider)}?returnUrl={Uri.EscapeDataString(returnUrl)}";

    public Task<DataResult<SessionInfo?>> SignInDevelopmentAsync(string subject, CancellationToken cancellationToken = default) =>
        CallAsync(() => api.SignInDevelopmentAsync(subject, cancellationToken), user => (SessionInfo?)new SessionInfo(user.UserId));

    public Task<DataResult> SignOutAsync(CancellationToken cancellationToken = default) => CallAsync(() => api.SignOutAsync(cancellationToken));
}
