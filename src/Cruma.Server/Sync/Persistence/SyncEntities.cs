using Cruma.Server.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cruma.Server.Sync.Persistence;

/// <summary>Záznam změnového feedu uživatele (plan.md N-2).</summary>
internal sealed class ChangeFeedEntryEntity : IUserOwned
{
    public Guid OwnerUserId { get; set; }

    public long Sequence { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public long? Version { get; set; }

    public bool Deleted { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Poslední přidělené pořadí feedu uživatele; zámek řádku zaručí monotónní pořadí.</summary>
internal sealed class ChangeFeedCounterEntity : IUserOwned
{
    public Guid OwnerUserId { get; set; }

    public long LastSequence { get; set; }
}

/// <summary>Zpracovaná změna synchronizace a její výsledek – každá změna nejvýš jednou (SYN-003).</summary>
internal sealed class ProcessedChangeEntity : IUserOwned
{
    public Guid OwnerUserId { get; set; }

    public Guid ChangeId { get; set; }

    public string ResultJson { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAtUtc { get; set; }
}

internal sealed class ChangeFeedEntryConfiguration : IEntityTypeConfiguration<ChangeFeedEntryEntity>
{
    public void Configure(EntityTypeBuilder<ChangeFeedEntryEntity> builder)
    {
        builder.ToTable("change_feed", "sync");
        builder.HasKey(entity => new { entity.OwnerUserId, entity.Sequence });
        builder.Property(entity => entity.EntityType).HasMaxLength(20);
    }
}

internal sealed class ChangeFeedCounterConfiguration : IEntityTypeConfiguration<ChangeFeedCounterEntity>
{
    public void Configure(EntityTypeBuilder<ChangeFeedCounterEntity> builder)
    {
        builder.ToTable("change_feed_counters", "sync");
        builder.HasKey(entity => entity.OwnerUserId);
    }
}

internal sealed class ProcessedChangeConfiguration : IEntityTypeConfiguration<ProcessedChangeEntity>
{
    public void Configure(EntityTypeBuilder<ProcessedChangeEntity> builder)
    {
        builder.ToTable("processed_changes", "sync");
        builder.HasKey(entity => new { entity.OwnerUserId, entity.ChangeId });
        builder.Property(entity => entity.ResultJson).HasColumnType("jsonb");
    }
}
