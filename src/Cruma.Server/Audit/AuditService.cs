using Cruma.Server.Audit.Persistence;
using Cruma.Server.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Server.Audit;

/// <summary>Zápis a dotazy auditu. Zapisuje jen přidáním řádku – žádná cesta záznam neupraví ani nesmaže (AUD-004).</summary>
internal sealed class AuditService(
    CrumaDbContext db,
    ICurrentUser currentUser,
    RequestContext requestContext,
    TimeProvider timeProvider) : IAuditService
{
    public void Record(AuditEvent auditEvent) => db.Set<AuditEventEntity>().Add(ToEntity(auditEvent));

    public async Task RecordNowAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        // Samostatný kontext, aby se událost uložila i tehdy, když se hlavní operace vrací zpět.
        var options = new DbContextOptionsBuilder<CrumaDbContext>();
        InfrastructureModule.ConfigureDbContext(options, db.Database.GetConnectionString());
        await using var auditContext = new CrumaDbContext(options.Options, new NoOwner());
        auditContext.Set<AuditEventEntity>().Add(ToEntity(auditEvent));
        await auditContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRecord>> FindAsync(AuditQuery query, CancellationToken cancellationToken)
    {
        var events = db.Set<AuditEventEntity>().AsNoTracking();
        if (query.UserId is { } userId)
        {
            events = events.Where(entity => entity.UserId == userId);
        }

        if (query.FromUtc is { } from)
        {
            events = events.Where(entity => entity.OccurredAtUtc >= from);
        }

        if (query.ToUtc is { } to)
        {
            events = events.Where(entity => entity.OccurredAtUtc < to);
        }

        if (query.OperationType is { } operationType)
        {
            events = events.Where(entity => entity.OperationType == operationType);
        }

        var entities = await events
            .OrderBy(entity => entity.OccurredAtUtc).ThenBy(entity => entity.Id)
            .Skip(query.Skip).Take(Math.Clamp(query.Take, 1, 1000))
            .ToListAsync(cancellationToken);

        return entities.Select(entity => new AuditRecord(
            entity.Id, entity.OccurredAtUtc, entity.UserId, entity.OperationType, entity.ObjectType, entity.ObjectId,
            entity.ClientType, Enum.Parse<AuditResult>(entity.Result, ignoreCase: true), entity.CorrelationId, entity.Attributes)).ToList();
    }

    private AuditEventEntity ToEntity(AuditEvent auditEvent)
    {
        var now = timeProvider.GetUtcNow();
        return new AuditEventEntity
        {
            Id = Guid.CreateVersion7(now),
            OccurredAtUtc = now,
            UserId = auditEvent.UserId ?? currentUser.UserId,
            OperationType = auditEvent.OperationType,
            ObjectType = auditEvent.ObjectType,
            ObjectId = auditEvent.ObjectId,
            ClientType = requestContext.ClientType.ToString().ToLowerInvariant(),
            Result = auditEvent.Result.ToString().ToLowerInvariant(),
            CorrelationId = requestContext.CorrelationId,
            Attributes = auditEvent.Attributes is null ? [] : new Dictionary<string, string>(auditEvent.Attributes),
        };
    }

    private sealed class NoOwner : ICurrentUser
    {
        public Guid? UserId => null;
    }
}
