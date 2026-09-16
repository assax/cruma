using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cruma.Desktop.Storage.Persistence;

/// <summary>Poznámka v lokální databázi. <see cref="ServerVersion"/> je poslední verze známá ze serveru – základ další změny (SYN-002).</summary>
public sealed class LocalNoteEntity
{
    public Guid Id { get; set; }

    public long? ServerVersion { get; set; }

    public string? Title { get; set; }

    public Guid CategoryId { get; set; }

    public List<Guid> TagIds { get; set; } = [];

    public string? Color { get; set; }

    public bool IsPinned { get; set; }

    public string State { get; set; } = string.Empty;

    public string Document { get; set; } = string.Empty;

    public bool HasConflict { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class LocalCategoryEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

public sealed class LocalTagEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Lokální mezilehlá verze poznámky za editační relaci (desktop-pattern.md §2, FR-6 akc. 2). Smaže se až po potvrzení
/// změn poznámky serverem (SYN-004).
/// </summary>
public sealed class LocalVersionEntity
{
    public long Id { get; set; }

    public Guid NoteId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public string Document { get; set; } = string.Empty;

    public string Metadata { get; set; } = string.Empty;
}

/// <summary>
/// Čekající změna (desktop-pattern.md §2.1). Obsah se sestaví až při odeslání z aktuálního lokálního stavu; odebere se
/// jen po potvrzení serverem (SYN-004). Odeslaná, ale nepotvrzená změna se už nemění – další úprava vytvoří novou.
/// </summary>
public sealed class PendingChangeEntity
{
    public long Sequence { get; set; }

    public Guid ChangeId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Operation { get; set; } = string.Empty;

    public DateTimeOffset ChangedAtUtc { get; set; }

    public DateTimeOffset? SentAtUtc { get; set; }

    public string? LastErrorCode { get; set; }
}

/// <summary>Stav synchronizace – jediný řádek: kurzor změnového feedu serveru (plan.md N-2).</summary>
public sealed class SyncStateEntity
{
    public int Id { get; set; } = 1;

    public long Cursor { get; set; }

    public DateTimeOffset? LastSyncedAtUtc { get; set; }
}

internal sealed class LocalNoteConfiguration : IEntityTypeConfiguration<LocalNoteEntity>
{
    public void Configure(EntityTypeBuilder<LocalNoteEntity> builder)
    {
        builder.ToTable("notes");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.TagIds).HasConversion(
            tags => JsonSerializer.Serialize(tags, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null) ?? new List<Guid>(),
            new ValueComparer<List<Guid>>((left, right) => left!.SequenceEqual(right!), tags => tags.Aggregate(0, HashCode.Combine), tags => tags.ToList()));
        builder.HasIndex(entity => new { entity.State, entity.UpdatedAtUtc });
        builder.HasIndex(entity => entity.CategoryId);
    }
}

internal sealed class LocalCategoryConfiguration : IEntityTypeConfiguration<LocalCategoryEntity>
{
    public void Configure(EntityTypeBuilder<LocalCategoryEntity> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(entity => entity.Id);
    }
}

internal sealed class LocalTagConfiguration : IEntityTypeConfiguration<LocalTagEntity>
{
    public void Configure(EntityTypeBuilder<LocalTagEntity> builder)
    {
        builder.ToTable("tags");
        builder.HasKey(entity => entity.Id);
    }
}

internal sealed class LocalVersionConfiguration : IEntityTypeConfiguration<LocalVersionEntity>
{
    public void Configure(EntityTypeBuilder<LocalVersionEntity> builder)
    {
        builder.ToTable("local_versions");
        builder.HasKey(entity => entity.Id);
        builder.HasIndex(entity => new { entity.NoteId, entity.UpdatedAtUtc });
    }
}

internal sealed class PendingChangeConfiguration : IEntityTypeConfiguration<PendingChangeEntity>
{
    public void Configure(EntityTypeBuilder<PendingChangeEntity> builder)
    {
        builder.ToTable("pending_changes");
        builder.HasKey(entity => entity.Sequence);
        builder.HasIndex(entity => entity.ChangeId).IsUnique();
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId });
    }
}

internal sealed class SyncStateConfiguration : IEntityTypeConfiguration<SyncStateEntity>
{
    public void Configure(EntityTypeBuilder<SyncStateEntity> builder)
    {
        builder.ToTable("sync_state");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
    }
}
