using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cruma.Server.Audit.Persistence;

/// <summary>
/// Auditní záznam ve schématu <c>audit</c>. Není filtrovaný vlastníkem – obsahuje i události bez uživatele
/// (neúspěšné přihlášení) a čte ho jen modul auditu s explicitním filtrem uživatele. Append-only hlídá trigger.
/// </summary>
internal sealed class AuditEventEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public Guid? UserId { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string ObjectType { get; set; } = string.Empty;

    public string? ObjectId { get; set; }

    public string ClientType { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public Dictionary<string, string> Attributes { get; set; } = [];
}

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEventEntity>
{
    public void Configure(EntityTypeBuilder<AuditEventEntity> builder)
    {
        builder.ToTable("audit_events", "audit");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.OperationType).HasMaxLength(100);
        builder.Property(entity => entity.ObjectType).HasMaxLength(50);
        builder.Property(entity => entity.ObjectId).HasMaxLength(100);
        builder.Property(entity => entity.ClientType).HasMaxLength(20);
        builder.Property(entity => entity.Result).HasMaxLength(20);
        builder.Property(entity => entity.CorrelationId).HasMaxLength(64);
        builder.Property(entity => entity.Attributes)
            .HasColumnType("jsonb")
            .HasConversion(
                attributes => JsonSerializer.Serialize(attributes, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>(),
                new ValueComparer<Dictionary<string, string>>(
                    (left, right) => left!.Count == right!.Count && !left.Except(right).Any(),
                    attributes => attributes.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
                    attributes => new Dictionary<string, string>(attributes)));
        builder.HasIndex(entity => new { entity.UserId, entity.OccurredAtUtc });
        builder.HasIndex(entity => new { entity.OperationType, entity.OccurredAtUtc });
        builder.HasIndex(entity => entity.OccurredAtUtc);
    }
}
