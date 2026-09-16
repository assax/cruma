using Cruma.Content;
using Cruma.Notes;

namespace Cruma.Ui.Services;

/// <summary>
/// Datová služba poznámek (ui-pattern.md §1, UI-002). Tvar odpovídá potřebám UI, ne endpointům: tenký klient ji
/// implementuje přes API klienta, desktop nad lokálním úložištěm a synchronizací.
/// </summary>
public interface INoteData
{
    Task<DataResult<PageResult<NoteItem>>> ListAsync(NoteState state, Guid? categoryId, Guid? tagId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<DataResult<NoteItem>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Vytvoří poznámku jen z obsahu do výchozí kategorie (FR-1, UI-005).</summary>
    Task<DataResult<NoteCreateResult>> CreateAsync(Guid id, ContentDocument document, CancellationToken cancellationToken = default);

    /// <summary>Uloží úpravu vůči verzi <paramref name="basedOn"/>; souběžné změny se sloučí na serveru (I1-D-1).</summary>
    Task<DataResult<NoteSaveResult>> SaveAsync(NoteItem basedOn, NoteChanges changes, CancellationToken cancellationToken = default);

    Task<DataResult<NoteSaveResult>> ChangeStateAsync(Guid id, NoteStateChange change, CancellationToken cancellationToken = default);

    Task<DataResult> DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Vyřeší konflikt bloku výběrem varianty nebo upraveným blokem (FR-28 akc. 3).</summary>
    Task<DataResult<NoteSaveResult>> ResolveConflictAsync(Guid id, long baseVersion, string blockId, ConflictSide? choice, ContentBlock? editedBlock, CancellationToken cancellationToken = default);
}

/// <summary>Kategorie a štítky (FR-3, FR-4).</summary>
public interface ICatalogData
{
    Task<DataResult<IReadOnlyList<CategoryItem>>> ListCategoriesAsync(CancellationToken cancellationToken = default);

    Task<DataResult<CategoryItem>> CreateCategoryAsync(string name, CancellationToken cancellationToken = default);

    Task<DataResult<CategoryItem>> RenameCategoryAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task<DataResult> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DataResult<IReadOnlyList<TagItem>>> ListTagsAsync(CancellationToken cancellationToken = default);

    Task<DataResult<TagItem>> CreateTagAsync(string name, CancellationToken cancellationToken = default);

    Task<DataResult<TagItem>> RenameTagAsync(Guid id, string name, CancellationToken cancellationToken = default);

    Task<DataResult> DeleteTagAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>Vyhledávání poznámek (FR-24).</summary>
public interface ISearchData
{
    Task<DataResult<PageResult<NoteItem>>> SearchAsync(string query, NoteState? state, int page, int pageSize, CancellationToken cancellationToken = default);
}

/// <summary>Přihlášení uživatele; <c>null</c> sezení znamená nepřihlášeného uživatele.</summary>
public sealed record SessionInfo(Guid UserId);

public sealed record SignInOptions(IReadOnlyList<string> ExternalProviders, bool DevelopmentSignIn);

public interface ISessionData
{
    Task<DataResult<SessionInfo?>> GetSessionAsync(CancellationToken cancellationToken = default);

    Task<DataResult<SignInOptions>> GetSignInOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Adresa, na kterou UI přesměruje pro přihlášení externím poskytovatelem (řeší shell).</summary>
    string ExternalSignInUrl(string provider, string returnUrl);

    Task<DataResult<SessionInfo?>> SignInDevelopmentAsync(string subject, CancellationToken cancellationToken = default);

    Task<DataResult> SignOutAsync(CancellationToken cancellationToken = default);
}
