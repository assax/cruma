using Cruma.Server.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Cruma.Server.Search.Persistence;

/// <summary>
/// Řádek indexu ve schématu <c>search</c>: tokeny z Cruma.Search a z nich <c>tsvector</c> v konfiguraci
/// <c>simple</c> bez databázové normalizace (search-pattern.md §4, SRC-001). Čte a zapisuje ho jen adaptér (PER-004).
/// </summary>
internal sealed class SearchIndexEntryEntity : IUserOwned
{
    public Guid OwnerUserId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string State { get; set; } = string.Empty;

    public string Tokens { get; set; } = string.Empty;

    public int NormalizerVersion { get; set; }

    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public DateTimeOffset IndexedAtUtc { get; set; }
}

internal sealed class SearchIndexEntryConfiguration : IEntityTypeConfiguration<SearchIndexEntryEntity>
{
    public void Configure(EntityTypeBuilder<SearchIndexEntryEntity> builder)
    {
        builder.ToTable("index_entries", "search");
        builder.HasKey(entity => new { entity.OwnerUserId, entity.EntityType, entity.EntityId });
        builder.Property(entity => entity.EntityType).HasMaxLength(20);
        builder.Property(entity => entity.State).HasMaxLength(20);
        builder.HasGeneratedTsVectorColumn(entity => entity.SearchVector, "simple", entity => new { entity.Tokens })
            .HasIndex(entity => entity.SearchVector)
            .HasMethod("GIN");
        builder.HasIndex(entity => entity.NormalizerVersion);
    }
}
