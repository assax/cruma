using Cruma.Content;
using Cruma.Notes;

namespace Cruma.Ui.Services;

/// <summary>Poznámka, jak ji zobrazuje UI: vlastnosti, dokument a verze, ze které vychází další úprava.</summary>
public sealed record NoteItem(Guid Id, long Version, Note Metadata, ContentDocument Document, bool HasConflict, DateTimeOffset UpdatedAtUtc);

/// <summary>Úprava poznámky z detailu: vlastnosti kromě stavu a dokument.</summary>
public sealed record NoteChanges(string? Title, Guid CategoryId, IReadOnlyList<Guid> TagIds, string? Color, bool IsPinned, ContentDocument Document);

/// <summary>Přepsaná vlastnost ohlášená po uložení (FR-28 akc. 6).</summary>
public sealed record OverwrittenValueInfo(string Field, string? OverwrittenValue, string? WinningValue);

/// <summary>Výsledek uložení: <see cref="Outcome"/> je <c>applied</c>, <c>merged</c> nebo <c>conflict</c>.</summary>
public sealed record NoteSaveResult(NoteItem Note, string Outcome, IReadOnlyList<OverwrittenValueInfo> OverwrittenValues, IReadOnlyList<string> Notices);

/// <summary>Výsledek vytvoření: uložená poznámka, nebo zařazení do fronty zápisů bez připojení (FR-30).</summary>
public sealed record NoteCreateResult(NoteItem? Note, bool Queued);

public sealed record CategoryItem(Guid Id, string Name, bool IsDefault);

public sealed record TagItem(Guid Id, string Name);

public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount);

/// <summary>Nová poznámka čekající ve frontě na odeslání (FR-30 akc. 2).</summary>
public sealed record PendingNote(Guid Id, string Preview, DateTimeOffset CreatedAtUtc);

public enum NoteStateChange
{
    Archive,
    Unarchive,
    Trash,
    Restore,
}

/// <summary>Chyba datové služby: kód, konkrétní pravidlo a zpráva pro uživatele (error-handling-policy.md P-5).</summary>
public sealed record DataError(string Code, string? Rule, string Message)
{
    public const string Offline = "offline";

    public bool IsOffline => Code == Offline;
}

public class DataResult
{
    protected DataResult(DataError? error) => Error = error;

    public DataError? Error { get; }

    public bool IsSuccess => Error is null;

    public static DataResult Success() => new(null);

    public static DataResult Failure(DataError error) => new(error);
}

public sealed class DataResult<T> : DataResult
{
    private readonly T? value;

    private DataResult(T? value, DataError? error)
        : base(error) => this.value = value;

    public T Value => IsSuccess ? value! : throw new InvalidOperationException($"Operace selhala: {Error!.Code}.");

    public static DataResult<T> Success(T value) => new(value, null);

    public static new DataResult<T> Failure(DataError error) => new(default, error);
}
