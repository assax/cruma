using System.Text.Json;
using Cruma.Notes;

namespace Cruma.Api.Contracts;

/// <summary>Stránka výsledků (API-005).</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>Poznámka. <see cref="Document"/> je dokument schématu Cruma.Content (CNT-006).</summary>
public sealed record NoteDto(
    Guid Id,
    long Version,
    string? Title,
    Guid CategoryId,
    IReadOnlyList<Guid> TagIds,
    string? Color,
    bool IsPinned,
    NoteState State,
    bool HasConflict,
    JsonElement Document,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

/// <summary>Vytvoření poznámky: jen identifikátor od klienta a obsah (FR-1, UI-005, SYN-006).</summary>
public sealed record CreateNoteRequest(Guid Id, JsonElement Document);

/// <summary>Úprava poznámky vůči verzi, ze které klient vycházel (I1-D-1). Stav se mění vlastními akcemi.</summary>
public sealed record UpdateNoteRequest(
    long BaseVersion,
    string? Title,
    Guid CategoryId,
    IReadOnlyList<Guid> TagIds,
    string? Color,
    bool IsPinned,
    JsonElement Document);

/// <summary>Vyřešení konfliktu: vybraná varianta (<c>current</c> / <c>incoming</c>) nebo ručně upravený blok.</summary>
public sealed record ResolveConflictRequest(long BaseVersion, string? Choice, JsonElement? EditedBlock);

public sealed record OverwrittenFieldDto(string Field, string? OverwrittenValue, string? WinningValue);

public sealed record MergeNoticeDto(string Kind, string? BlockId);

/// <summary>Výsledek zápisu: <see cref="Outcome"/> je <c>applied</c>, <c>merged</c> nebo <c>conflict</c>.</summary>
public sealed record NoteWriteResponse(
    NoteDto Note,
    string Outcome,
    IReadOnlyList<OverwrittenFieldDto> OverwrittenFields,
    IReadOnlyList<MergeNoticeDto> Notices);

public sealed record CategoryDto(Guid Id, string Name, bool IsDefault);

public sealed record CreateCategoryRequest(Guid Id, string Name);

public sealed record TagDto(Guid Id, string Name);

public sealed record CreateTagRequest(Guid Id, string Name);

public sealed record RenameRequest(string Name);

public sealed record CurrentUserDto(Guid UserId);

public sealed record DevelopmentSignInRequest(string Subject);
