using Cruma.Desktop.Storage.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cruma.Desktop.Storage;

/// <summary>
/// Lokální databáze desktopu – jeden soubor SQLite na přihlášeného uživatele (desktop-pattern.md §2, PER-001).
/// Neobsahuje tokeny ani jiné tajné údaje (PER-005).
/// </summary>
public sealed class CrumaLocalDbContext(DbContextOptions<CrumaLocalDbContext> options) : DbContext(options)
{
    public DbSet<LocalNoteEntity> Notes => Set<LocalNoteEntity>();

    public DbSet<LocalCategoryEntity> Categories => Set<LocalCategoryEntity>();

    public DbSet<LocalTagEntity> Tags => Set<LocalTagEntity>();

    public DbSet<LocalVersionEntity> LocalVersions => Set<LocalVersionEntity>();

    public DbSet<PendingChangeEntity> PendingChanges => Set<PendingChangeEntity>();

    public DbSet<SyncStateEntity> SyncState => Set<SyncStateEntity>();

    public static DbContextOptions<CrumaLocalDbContext> OptionsFor(string databasePath) =>
        new DbContextOptionsBuilder<CrumaLocalDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrumaLocalDbContext).Assembly);
}

/// <summary>Kontext pro <c>dotnet ef migrations</c>.</summary>
internal sealed class DesignTimeLocalDbContextFactory : IDesignTimeDbContextFactory<CrumaLocalDbContext>
{
    public CrumaLocalDbContext CreateDbContext(string[] args) => new(CrumaLocalDbContext.OptionsFor("design-time.db"));
}
