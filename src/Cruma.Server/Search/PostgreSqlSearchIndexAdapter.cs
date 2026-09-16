using Cruma.Search;
using Cruma.Server.Infrastructure;
using Cruma.Server.Search.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Server.Search;

/// <summary>
/// Adaptér indexu nad PostgreSQL (search-pattern.md §4): ukládá tokeny, dotaz je <c>to_tsquery('simple', 'a:* &amp; b:*')</c>.
/// Omezení na aktuálního uživatele zajišťuje globální filtr vlastníka přímo v dotazu adaptéru (SRC-004, PER-002).
/// </summary>
internal sealed class PostgreSqlSearchIndexAdapter(CrumaDbContext db, TimeProvider timeProvider) : ISearchIndexAdapter
{
    private const string TextSearchConfiguration = "simple";

    public async Task UpsertAsync(SearchIndexEntry entry, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(entry.EntityType, entry.EntityId, cancellationToken);
        if (existing is null)
        {
            existing = new SearchIndexEntryEntity { EntityType = entry.EntityType, EntityId = entry.EntityId };
            db.Set<SearchIndexEntryEntity>().Add(existing);
        }

        existing.State = entry.State;
        existing.Tokens = Tokenizer.ToIndexString(entry.Tokens);
        existing.NormalizerVersion = TextNormalizer.Version;
        existing.IndexedAtUtc = timeProvider.GetUtcNow();
    }

    public async Task RemoveAsync(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        var existing = await FindAsync(entityType, entityId, cancellationToken);
        if (existing is not null)
        {
            db.Set<SearchIndexEntryEntity>().Remove(existing);
        }
    }

    // Nejdřív položky sledované v probíhající jednotce práce, pak databáze.
    private async Task<SearchIndexEntryEntity?> FindAsync(string entityType, Guid entityId, CancellationToken cancellationToken) =>
        db.Set<SearchIndexEntryEntity>().Local.FirstOrDefault(entity => entity.EntityType == entityType && entity.EntityId == entityId)
        ?? await db.Set<SearchIndexEntryEntity>()
            .SingleOrDefaultAsync(entity => entity.EntityType == entityType && entity.EntityId == entityId, cancellationToken);

    public async Task<SearchResultPage> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        if (request.Query.IsEmpty)
        {
            return new SearchResultPage([], 0);
        }

        // Tokeny obsahují jen písmena a číslice (Tokenizer), takže v tsquery nemohou tvořit operátory.
        var tsQuery = string.Join(" & ", request.Query.Tokens.Select(token => token + ":*"));

        var matches = db.Set<SearchIndexEntryEntity>().AsNoTracking()
            .Where(entity => entity.EntityType == request.EntityType)
            .Where(entity => entity.SearchVector.Matches(EF.Functions.ToTsQuery(TextSearchConfiguration, tsQuery)));
        if (request.State is { } state)
        {
            matches = matches.Where(entity => entity.State == state);
        }

        var total = await matches.CountAsync(cancellationToken);
        var ids = await matches
            .OrderByDescending(entity => entity.SearchVector.Rank(EF.Functions.ToTsQuery(TextSearchConfiguration, tsQuery)))
            .ThenByDescending(entity => entity.IndexedAtUtc)
            .ThenBy(entity => entity.EntityId)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(entity => entity.EntityId)
            .ToListAsync(cancellationToken);

        return new SearchResultPage(ids, total);
    }
}
