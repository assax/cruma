using Cruma.Server.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cruma.Server.Notes.Persistence;

internal sealed class CategoryEntity : IUserOwned
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class TagEntity : IUserOwned
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>Poznámka s aktuálním stavem; odkazuje na aktuální verzi (server-pattern.md §3).</summary>
internal sealed class NoteEntity : IUserOwned
{
    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public long CurrentVersion { get; set; }

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

/// <summary>Verze poznámky (versioning-and-sync-pattern.md §2.2). <see cref="ClientInstanceId"/> slouží ke spojování uložení (N-3).</summary>
internal sealed class NoteVersionEntity : IUserOwned
{
    public Guid NoteId { get; set; }

    public long Number { get; set; }

    public Guid OwnerUserId { get; set; }

    public long? BaseNumber { get; set; }

    public string Source { get; set; } = string.Empty;

    public Guid? ClientInstanceId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public string Document { get; set; } = string.Empty;

    public string Metadata { get; set; } = string.Empty;

    public string OverwrittenValues { get; set; } = "[]";

    public string Notices { get; set; } = "[]";

    public bool HasConflict { get; set; }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<CategoryEntity>
{
    public void Configure(EntityTypeBuilder<CategoryEntity> builder)
    {
        builder.ToTable("categories", "notes");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.HasIndex(entity => entity.OwnerUserId).IsUnique().HasFilter("is_default");
    }
}

internal sealed class TagConfiguration : IEntityTypeConfiguration<TagEntity>
{
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable("tags", "notes");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Name).HasMaxLength(200);
        builder.HasIndex(entity => entity.OwnerUserId);
    }
}

internal sealed class NoteConfiguration : IEntityTypeConfiguration<NoteEntity>
{
    public void Configure(EntityTypeBuilder<NoteEntity> builder)
    {
        builder.ToTable("notes", "notes");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Title).HasMaxLength(1000);
        builder.Property(entity => entity.Color).HasMaxLength(32);
        builder.Property(entity => entity.State).HasMaxLength(20);
        builder.Property(entity => entity.Document).HasColumnType("jsonb");
        builder.HasIndex(entity => new { entity.OwnerUserId, entity.State, entity.UpdatedAtUtc });
        builder.HasIndex(entity => new { entity.OwnerUserId, entity.CategoryId });
    }
}

internal sealed class NoteVersionConfiguration : IEntityTypeConfiguration<NoteVersionEntity>
{
    public void Configure(EntityTypeBuilder<NoteVersionEntity> builder)
    {
        builder.ToTable("note_versions", "notes");
        builder.HasKey(entity => new { entity.NoteId, entity.Number });
        builder.Property(entity => entity.Source).HasMaxLength(20);
        builder.Property(entity => entity.Document).HasColumnType("jsonb");
        builder.Property(entity => entity.Metadata).HasColumnType("jsonb");
        builder.Property(entity => entity.OverwrittenValues).HasColumnType("jsonb");
        builder.Property(entity => entity.Notices).HasColumnType("jsonb");
        builder.HasOne<NoteEntity>().WithMany().HasForeignKey(entity => entity.NoteId).OnDelete(DeleteBehavior.Cascade);
    }
}
