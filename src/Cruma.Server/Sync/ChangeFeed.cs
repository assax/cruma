using Cruma.Server.Infrastructure;
using Cruma.Server.Sync.Persistence;
using Cruma.Sync;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Server.Sync;

/// <summary>Záznam feedu vrácený dotazem.</summary>
public sealed record FeedEntry(long Sequence, SyncEntityType EntityType, Guid EntityId, long? Version, bool Deleted);

/// <summary>Změnový feed uživatele (plan.md N-2) – veřejné rozhraní pro zápisovou cestu a synchronizaci.</summary>
public interface IChangeFeed
{
    /// <summary>
    /// Zapíše změnu entity s dalším pořadím uživatele. Musí běžet v transakci zápisu – zámek počítadla drží
    /// pořadí monotónní i při souběžných zápisech.
    /// </summary>
    Task AppendAsync(SyncEntityType entityType, Guid entityId, long? version, bool deleted, CancellationToken cancellationToken);

    /// <summary>Záznamy s pořadím vyšším než <paramref name="cursor"/> vzestupně.</summary>
    Task<IReadOnlyList<FeedEntry>> ReadAfterAsync(long cursor, int maxEntries, CancellationToken cancellationToken);
}

internal sealed class ChangeFeed(CrumaDbContext db, ICurrentUser currentUser, TimeProvider timeProvider) : IChangeFeed
{
    public async Task AppendAsync(SyncEntityType entityType, Guid entityId, long? version, bool deleted, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Zápis do změnového feedu musí běžet v transakci.");
        }

        var userId = currentUser.UserId ?? throw new InvalidOperationException("Změnový feed vyžaduje přihlášeného uživatele.");

        // Atomické zvýšení počítadla se zámkem řádku do konce transakce.
        var sequences = await db.Database
            .SqlQuery<long>($"""
                INSERT INTO sync.change_feed_counters (owner_user_id, last_sequence) VALUES ({userId}, 1)
                ON CONFLICT (owner_user_id) DO UPDATE SET last_sequence = sync.change_feed_counters.last_sequence + 1
                RETURNING last_sequence AS "Value"
                """)
            .ToListAsync(cancellationToken);
        var sequence = sequences.Single();

        db.Set<ChangeFeedEntryEntity>().Add(new ChangeFeedEntryEntity
        {
            OwnerUserId = userId,
            Sequence = sequence,
            EntityType = entityType.ToString(),
            EntityId = entityId,
            Version = version,
            Deleted = deleted,
            CreatedAtUtc = timeProvider.GetUtcNow(),
        });
    }

    public async Task<IReadOnlyList<FeedEntry>> ReadAfterAsync(long cursor, int maxEntries, CancellationToken cancellationToken)
    {
        var entities = await db.Set<ChangeFeedEntryEntity>().AsNoTracking()
            .Where(entity => entity.Sequence > cursor)
            .OrderBy(entity => entity.Sequence)
            .Take(maxEntries)
            .ToListAsync(cancellationToken);

        return entities
            .Select(entity => new FeedEntry(entity.Sequence, Enum.Parse<SyncEntityType>(entity.EntityType), entity.EntityId, entity.Version, entity.Deleted))
            .ToList();
    }
}
