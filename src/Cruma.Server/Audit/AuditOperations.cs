namespace Cruma.Server.Audit;

/// <summary>Typy auditních operací I-1 (logging-and-audit-policy.md §2).</summary>
public static class AuditOperations
{
    public const string SignIn = "auth.sign_in";
    public const string SignOut = "auth.sign_out";
    public const string SignInFailed = "auth.sign_in_failed";
    public const string ProviderLinked = "identity.provider_linked";

    public const string NoteCreated = "note.created";
    public const string NoteUpdated = "note.updated";
    public const string NoteArchived = "note.archived";
    public const string NoteTrashed = "note.trashed";
    public const string NoteRestored = "note.restored";
    public const string NoteDeletedPermanently = "note.deleted_permanently";
    public const string NoteConflictResolved = "note.conflict_resolved";

    public const string SyncSession = "sync.session";
    public const string SyncChangeRejected = "sync.change_rejected";

    public const string RateLimited = "security.rate_limited";
    public const string AuthorizationDenied = "security.authorization_denied";
}

/// <summary>Typy objektů auditu.</summary>
public static class AuditObjectTypes
{
    public const string User = "user";
    public const string Note = "note";
    public const string Device = "device";
    public const string Request = "request";
}

public enum AuditResult
{
    Success,
    Failure,
}
