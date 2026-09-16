using Cruma.Content;
using Cruma.Notes;

namespace Cruma.Desktop.Storage;

/// <summary>Poznámka v lokálním úložišti; <see cref="ServerVersion"/> <c>null</c> = na serveru ještě není.</summary>
public sealed record LocalNote(Guid Id, long? ServerVersion, Note Metadata, ContentDocument Document, bool HasConflict, DateTimeOffset UpdatedAtUtc);

/// <summary>Úprava poznámky z detailu (vlastnosti kromě stavu a dokument).</summary>
public sealed record LocalNoteChanges(string? Title, Guid CategoryId, IReadOnlyList<Guid> TagIds, string? Color, bool IsPinned, ContentDocument Document);

public sealed record LocalCategory(Guid Id, string Name, bool IsDefault);

public sealed record LocalTag(Guid Id, string Name);

public sealed record LocalPage<T>(IReadOnlyList<T> Items, int TotalCount);

public enum LocalStateChange
{
    Archive,
    Unarchive,
    Trash,
    Restore,
}

/// <summary>Výsledek operace lokálního úložiště; <see cref="ErrorCode"/> je kód pravidla (např. <c>note_not_in_trash</c>).</summary>
public sealed class StoreResult<T>
{
    private StoreResult(T? value, string? errorCode, string? message)
    {
        ValueOrDefault = value;
        ErrorCode = errorCode;
        Message = message;
    }

    public T? ValueOrDefault { get; }

    public T Value => ErrorCode is null ? ValueOrDefault! : throw new InvalidOperationException($"Operace selhala: {ErrorCode}.");

    public string? ErrorCode { get; }

    public string? Message { get; }

    public bool IsSuccess => ErrorCode is null;

    public static StoreResult<T> Success(T value) => new(value, null, null);

    public static StoreResult<T> Failure(string code, string message) => new(default, code, message);
}

/// <summary>Konfigurace lokálního úložiště.</summary>
public sealed class LocalStoreOptions
{
    /// <summary>Interval nečinnosti, po kterém lokální editační relace končí a další úprava založí novou lokální verzi (N-3).</summary>
    public TimeSpan EditSessionInterval { get; set; } = TimeSpan.FromMinutes(5);
}

public static class LocalErrorCodes
{
    public const string NotFound = "not_found";
    public const string DefaultCategoryMissing = "default_category_missing";
    public const string CategoryNotFound = "category_not_found";
    public const string TagNotFound = "tag_not_found";
    public const string ConflictResolutionInvalid = "conflict_resolution_invalid";
}
