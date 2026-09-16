using Cruma.Content;
using Cruma.Notes;
using Cruma.Search;

namespace Cruma.Server.Search;

/// <summary>Veřejné rozhraní modulu vyhledávání pro ostatní moduly (DEP-007).</summary>
public interface ISearchService
{
    /// <summary>Zaindexuje název a prostý text poznámky (FR-24 akc. 5, CNT-007).</summary>
    Task IndexNoteAsync(Guid noteId, string? title, ContentDocument document, NoteState state, CancellationToken cancellationToken);

    Task RemoveNoteAsync(Guid noteId, CancellationToken cancellationToken);

    /// <summary>Najde poznámky aktuálního uživatele; <paramref name="state"/> <c>null</c> = všechny stavy.</summary>
    Task<SearchResultPage> SearchNotesAsync(string? query, NoteState? state, int skip, int take, CancellationToken cancellationToken);
}

internal sealed class SearchService(ISearchIndexAdapter adapter) : ISearchService
{
    public Task IndexNoteAsync(Guid noteId, string? title, ContentDocument document, NoteState state, CancellationToken cancellationToken) =>
        adapter.UpsertAsync(
            SearchIndexEntry.FromText(SearchEntityTypes.Note, noteId, StateName(state), PlainTextExtractor.Extract(title, document)),
            cancellationToken);

    public Task RemoveNoteAsync(Guid noteId, CancellationToken cancellationToken) =>
        adapter.RemoveAsync(SearchEntityTypes.Note, noteId, cancellationToken);

    public Task<SearchResultPage> SearchNotesAsync(string? query, NoteState? state, int skip, int take, CancellationToken cancellationToken) =>
        adapter.SearchAsync(
            new SearchRequest(SearchQuery.Parse(query), SearchEntityTypes.Note, state is { } value ? StateName(value) : null, skip, take),
            cancellationToken);

    internal static string StateName(NoteState state) => state.ToString().ToLowerInvariant();
}

public static class SearchModule
{
    public static IServiceCollection AddSearchModule(this IServiceCollection services) =>
        services
            .AddScoped<ISearchIndexAdapter, PostgreSqlSearchIndexAdapter>()
            .AddScoped<ISearchService, SearchService>();
}
