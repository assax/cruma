using Cruma.Search;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Desktop.Storage.Search;

/// <summary>
/// Adaptér indexu nad SQLite FTS5 (search-pattern.md §4). Tabulka používá tokenizer <c>ascii</c>, který ne-ASCII znaky
/// bere jako součást slova a dělí jen na ASCII oddělovačích – ukládají se tokeny z Cruma.Search mezerou oddělené, bez
/// vlastní normalizace nebo odstraňování diakritiky v SQLite (SRC-001). Dotaz: každý token jako prefix, všechny současně.
/// Index čte a zapisuje jen tento adaptér (PER-004). Databáze patří jednomu uživateli, filtr uživatele není potřeba.
/// Řádek indexu se k entitě dohledává přes tabulku <c>search_index_rows</c> (rowid), protože filtr podle neindexovaného
/// sloupce FTS5 prochází celou tabulku.
/// </summary>
public sealed class SqliteSearchIndexAdapter(CrumaLocalDbContext db) : ISearchIndexAdapter
{
    public async Task UpsertAsync(SearchIndexEntry entry, CancellationToken cancellationToken)
    {
        await RemoveAsync(entry.EntityType, entry.EntityId, cancellationToken);
        var tokens = Tokenizer.ToIndexString(entry.Tokens);
        var entityId = entry.EntityId.ToString();
        // Jeden příkaz: last_insert_rowid() platí jen na stejném připojení.
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO search_index (entity_type, entity_id, state, normalizer_version, tokens) VALUES ({entry.EntityType}, {entityId}, {entry.State}, {TextNormalizer.Version}, {tokens});
            INSERT OR REPLACE INTO search_index_rows (entity_type, entity_id, fts_rowid) VALUES ({entry.EntityType}, {entityId}, last_insert_rowid());
            """,
            cancellationToken);
    }

    public async Task RemoveAsync(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        var id = entityId.ToString();
        await db.Database.ExecuteSqlAsync(
            $"DELETE FROM search_index WHERE rowid IN (SELECT fts_rowid FROM search_index_rows WHERE entity_type = {entityType} AND entity_id = {id})",
            cancellationToken);
        await db.Database.ExecuteSqlAsync(
            $"DELETE FROM search_index_rows WHERE entity_type = {entityType} AND entity_id = {id}",
            cancellationToken);
    }

    public async Task<SearchResultPage> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        if (request.Query.IsEmpty)
        {
            return new SearchResultPage([], 0);
        }

        // Tokeny obsahují jen písmena a číslice, uvozovky v nich nejsou.
        var match = string.Join(" AND ", request.Query.Tokens.Select(token => $"tokens : \"{token}\"*"));
        var state = request.State ?? string.Empty;

        var total = (await db.Database.SqlQuery<int>(
            $"SELECT COUNT(*) AS Value FROM search_index WHERE search_index MATCH {match} AND entity_type = {request.EntityType} AND ({state} = '' OR state = {state})")
            .ToListAsync(cancellationToken)).Single();

        var ids = await db.Database.SqlQuery<string>(
            $"SELECT entity_id AS Value FROM search_index WHERE search_index MATCH {match} AND entity_type = {request.EntityType} AND ({state} = '' OR state = {state}) ORDER BY bm25(search_index), entity_id LIMIT {request.Take} OFFSET {request.Skip}")
            .ToListAsync(cancellationToken);

        return new SearchResultPage([.. ids.Select(Guid.Parse)], total);
    }
}
