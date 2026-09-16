namespace Cruma.Server.Infrastructure;

/// <summary>Stabilní kódy chyb v rozšíření <c>code</c> ProblemDetails (error-handling-policy.md §2, ERR-001).</summary>
public static class ErrorCodes
{
    public const string ValidationFailed = "validation_failed";
    public const string NotFound = "not_found";
    public const string VersionConflict = "version_conflict";
    public const string ClientVersionUnsupported = "client_version_unsupported";
    public const string BlobMissing = "blob_missing";
    public const string Unauthenticated = "unauthenticated";
    public const string Forbidden = "forbidden";
    public const string InternalError = "internal_error";

    /// <summary>HTTP stav pro kód chyby.</summary>
    public static int StatusFor(string code) => code switch
    {
        ValidationFailed => StatusCodes.Status400BadRequest,
        NotFound => StatusCodes.Status404NotFound,
        VersionConflict => StatusCodes.Status409Conflict,
        ClientVersionUnsupported => StatusCodes.Status426UpgradeRequired,
        BlobMissing => StatusCodes.Status422UnprocessableEntity,
        Unauthenticated => StatusCodes.Status401Unauthorized,
        Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Kód chyby pro HTTP stav bez vlastního kódu (odpovědi autentizace, směrování, neošetřená výjimka).</summary>
    public static string ForStatus(int status) => status switch
    {
        StatusCodes.Status400BadRequest => ValidationFailed,
        StatusCodes.Status401Unauthorized => Unauthenticated,
        StatusCodes.Status403Forbidden => Forbidden,
        StatusCodes.Status404NotFound => NotFound,
        StatusCodes.Status405MethodNotAllowed => NotFound,
        StatusCodes.Status409Conflict => VersionConflict,
        StatusCodes.Status426UpgradeRequired => ClientVersionUnsupported,
        _ => InternalError,
    };
}
