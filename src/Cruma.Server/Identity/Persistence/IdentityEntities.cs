using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cruma.Server.Identity.Persistence;

/// <summary>Uživatel Cruma s interním identifikátorem (SEC-001). Systémová tabulka modulu identity, bez filtru vlastníka.</summary>
internal sealed class UserEntity
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? LastSignInAtUtc { get; set; }
}

/// <summary>
/// Navázaná externí identita: poskytovatel a jeho identifikátor subjektu. Nový poskytovatel je jen nová hodnota
/// <see cref="Provider"/>, ne změna schématu (NFR-14, FR-32 akc. 2).
/// </summary>
internal sealed class LinkedIdentityEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset LinkedAtUtc { get; set; }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users", "identity");
        builder.HasKey(entity => entity.Id);
    }
}

internal sealed class LinkedIdentityConfiguration : IEntityTypeConfiguration<LinkedIdentityEntity>
{
    public void Configure(EntityTypeBuilder<LinkedIdentityEntity> builder)
    {
        builder.ToTable("linked_identities", "identity");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Provider).HasMaxLength(50);
        builder.Property(entity => entity.Subject).HasMaxLength(255);
        builder.HasIndex(entity => new { entity.Provider, entity.Subject }).IsUnique();
        builder.HasIndex(entity => entity.UserId);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
