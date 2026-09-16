namespace Cruma.Server.Audit;

/// <summary>
/// Auditní událost bez obsahu (AUD-002): operace, objekt a malé atributy (identifikátory, kódy). Čas, uživatel,
/// typ klienta a correlation id doplní modul auditu z kontextu požadavku.
/// </summary>
public sealed record AuditEvent(
    string OperationType,
    string ObjectType,
    string? ObjectId,
    AuditResult Result = AuditResult.Success,
    IReadOnlyDictionary<string, string>? Attributes = null,
    Guid? UserId = null);

/// <summary>Uložený auditní záznam.</summary>
public sealed record AuditRecord(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid? UserId,
    string OperationType,
    string ObjectType,
    string? ObjectId,
    string ClientType,
    AuditResult Result,
    string CorrelationId,
    IReadOnlyDictionary<string, string> Attributes);

/// <summary>Dotaz na audit podle uživatele, času a typu operace (FR-35 akc. 3).</summary>
public sealed record AuditQuery(Guid? UserId, DateTimeOffset? FromUtc, DateTimeOffset? ToUtc, string? OperationType, int Skip = 0, int Take = 100);

/// <summary>Veřejné rozhraní modulu auditu pro ostatní moduly (DEP-007, AUD-001).</summary>
public interface IAuditService
{
    /// <summary>Přidá událost do probíhající jednotky práce; uloží se spolu s operací, kterou popisuje.</summary>
    void Record(AuditEvent auditEvent);

    /// <summary>Uloží událost okamžitě, samostatně (např. neúspěšné přihlášení, odmítnutá změna).</summary>
    Task RecordNowAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditRecord>> FindAsync(AuditQuery query, CancellationToken cancellationToken);
}
