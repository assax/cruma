using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Server.Infrastructure;

/// <summary>
/// Jediný DbContext serveru (server-pattern.md §3). Moduly přispívají konfiguracemi entit ve vlastních schématech
/// (NAM-003). Entity <see cref="IUserOwned"/> filtruje globální filtr aktuálního uživatele; obejití je jen
/// pojmenované přes <see cref="AcrossAllUsers{TEntity}"/> (PER-002).
/// </summary>
public sealed class CrumaDbContext(DbContextOptions<CrumaDbContext> options, ICurrentUser currentUser) : DbContext(options)
{
    /// <summary>Název globálního filtru vlastníka.</summary>
    public const string OwnerFilter = "Owner";

    /// <summary>Aktuální uživatel pro filtr; bez přihlášení prázdný identifikátor, který nic nenajde.</summary>
    internal Guid CurrentUserIdForFilter => currentUser.UserId ?? Guid.Empty;

    /// <summary>
    /// Pojmenované obejití filtru vlastníka pro práci napříč uživateli. <paramref name="reason"/> je povinné
    /// zdůvodnění, které dokumentuje schválenou výjimku v kódu.
    /// </summary>
    internal IQueryable<TEntity> AcrossAllUsers<TEntity>(string reason)
        where TEntity : class, IUserOwned
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return Set<TEntity>().IgnoreQueryFilters([OwnerFilter]);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CrumaDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => typeof(IUserOwned).IsAssignableFrom(type.ClrType)).ToList())
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var filter = Expression.Lambda(
                Expression.Equal(
                    Expression.Property(parameter, nameof(IUserOwned.OwnerUserId)),
                    Expression.Property(Expression.Constant(this), nameof(CurrentUserIdForFilter))),
                parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(OwnerFilter, filter);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        EnforceOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Nová entita dostane vlastníka z aktuálního uživatele; zápis entity jiného uživatele je chyba (PER-002).
    private void EnforceOwnership()
    {
        foreach (var entry in ChangeTracker.Entries<IUserOwned>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var userId = currentUser.UserId
                ?? throw new InvalidOperationException("Zápis uživatelských dat bez přihlášeného uživatele.");

            if (entry.State == EntityState.Added && entry.Entity.OwnerUserId == Guid.Empty)
            {
                entry.Entity.OwnerUserId = userId;
            }

            if (entry.Entity.OwnerUserId != userId)
            {
                throw new InvalidOperationException(
                    $"Zápis entity {entry.Metadata.ClrType.Name} jiného uživatele není povolený (PER-002).");
            }
        }
    }
}
