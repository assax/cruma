using Cruma.Content;
using Cruma.Notes;
using Cruma.Server.Infrastructure;
using Cruma.Versioning;

namespace Cruma.Server.Notes;

/// <summary>Vlastnosti poznámky zaslané klientem; <see cref="State"/> <c>null</c> ponechá stav beze změny.</summary>
public sealed record NoteMetadataInput(
    string? Title,
    Guid CategoryId,
    IReadOnlyCollection<Guid> TagIds,
    string? Color,
    bool IsPinned,
    NoteState? State);

/// <summary>Aktuální stav poznámky.</summary>
public sealed record NoteView(
    Guid Id,
    long Version,
    Note Metadata,
    ContentDocument Document,
    bool HasConflict,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>Výsledek zápisu poznámky: nový stav, jak byla změna přijata, přepsané hodnoty a upozornění.</summary>
public sealed record NoteWriteResult(
    NoteView Note,
    NoteMergeOutcome Outcome,
    IReadOnlyList<OverwrittenValue> OverwrittenValues,
    IReadOnlyList<MergeNotice> Notices);

public sealed record CategoryView(Guid Id, string Name, bool IsDefault);

public sealed record TagView(Guid Id, string Name);

public sealed record PagedList<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>Vytvoření poznámky s identifikátorem od klienta; opakované vytvoření je idempotentní (SYN-003, SYN-006).</summary>
public sealed record CreateNoteCommand(Guid NoteId, ContentDocument Document, NoteMetadataInput? Metadata = null, DateTimeOffset? ChangedAtUtc = null);

/// <summary>Uložení poznámky vůči základní verzi, ze které klient vycházel (I1-D-1, SYN-002).</summary>
public sealed record SaveNoteCommand(Guid NoteId, long BaseVersion, ContentDocument Document, NoteMetadataInput Metadata, DateTimeOffset? ChangedAtUtc = null);

/// <summary>Vyřešení konfliktu bloku výběrem varianty nebo upraveným blokem (FR-28 akc. 3).</summary>
public sealed record ResolveConflictCommand(Guid NoteId, long BaseVersion, string BlockId, ConflictSide? Choice, ContentBlock? EditedBlock);

public enum NoteStateChange
{
    Archive,
    Unarchive,
    Trash,
    Restore,
}

public sealed record NoteListQuery(NoteState State, Guid? CategoryId, Guid? TagId, int Skip, int Take);

/// <summary>
/// Veřejné rozhraní modulu poznámek. Všechny zápisy poznámek, kategorií a štítků – z REST i ze synchronizace –
/// procházejí touto službou (plan.md N-1, DEP-007).
/// </summary>
public interface INotesService
{
    Task<ServiceResult<NoteWriteResult>> CreateNoteAsync(CreateNoteCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<NoteWriteResult>> SaveNoteAsync(SaveNoteCommand command, CancellationToken cancellationToken);

    Task<ServiceResult<NoteWriteResult>> ChangeNoteStateAsync(Guid noteId, NoteStateChange change, CancellationToken cancellationToken);

    Task<ServiceResult<NoteWriteResult>> ResolveConflictAsync(ResolveConflictCommand command, CancellationToken cancellationToken);

    Task<ServiceResult> DeleteNotePermanentlyAsync(Guid noteId, CancellationToken cancellationToken);

    Task<ServiceResult<NoteView>> GetNoteAsync(Guid noteId, CancellationToken cancellationToken);

    Task<PagedList<NoteView>> ListNotesAsync(NoteListQuery query, CancellationToken cancellationToken);

    /// <summary>Poznámky aktuálního uživatele v pořadí zadaných identifikátorů; neexistující se vynechají.</summary>
    Task<IReadOnlyList<NoteView>> GetNotesAsync(IReadOnlyList<Guid> noteIds, CancellationToken cancellationToken);

    /// <summary>Vyhledá poznámky v názvu a obsahu (FR-24) s volitelným filtrem stavu a vrátí je v pořadí relevance.</summary>
    Task<PagedList<NoteView>> SearchNotesAsync(string? query, NoteState? state, int skip, int take, CancellationToken cancellationToken);

    Task<ServiceResult<CategoryView>> CreateCategoryAsync(Guid categoryId, string name, CancellationToken cancellationToken);

    Task<ServiceResult<CategoryView>> RenameCategoryAsync(Guid categoryId, string name, CancellationToken cancellationToken);

    Task<ServiceResult> DeleteCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<ServiceResult<CategoryView>> GetCategoryAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<PagedList<CategoryView>> ListCategoriesAsync(int skip, int take, CancellationToken cancellationToken);

    Task<ServiceResult<TagView>> CreateTagAsync(Guid tagId, string name, CancellationToken cancellationToken);

    Task<ServiceResult<TagView>> RenameTagAsync(Guid tagId, string name, CancellationToken cancellationToken);

    Task<ServiceResult> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken);

    Task<ServiceResult<TagView>> GetTagAsync(Guid tagId, CancellationToken cancellationToken);

    Task<PagedList<TagView>> ListTagsAsync(int skip, int take, CancellationToken cancellationToken);

    /// <summary>Vytvoří výchozí kategorii aktuálního uživatele, pokud ještě neexistuje (FR-3 akc. 5, plan.md N-4).</summary>
    Task<CategoryView> EnsureDefaultCategoryAsync(CancellationToken cancellationToken);
}
