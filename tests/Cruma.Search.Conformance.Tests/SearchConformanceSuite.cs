using Cruma.Search;

namespace Cruma.Search.Conformance.Tests;

/// <summary>Adaptér pod testem a potvrzení jeho zápisů (zápisy adaptéru patří do jednotky práce volajícího).</summary>
public interface ISearchConformanceTarget : IAsyncDisposable
{
    ISearchIndexAdapter Adapter { get; }

    Task CommitAsync();
}

/// <summary>
/// Společná konformní sada vyhledávání (TST-002, testing-strategy.md §4): stejná data a dotazy musí nad každým
/// adaptérem vrátit stejnou množinu výsledků – totožnou s referenčním vyhodnocením <see cref="SearchQuery.Matches(string?)"/>.
/// </summary>
public abstract class SearchConformanceSuite
{
    /// <summary>Fixtura: český text s diakritikou, velkými písmeny, číslicemi a interpunkcí.</summary>
    protected static readonly IReadOnlyList<(int Number, string State, string Text)> Documents =
    [
        (1, "active", "Certifikát serveru vyprší 31. 12."),
        (2, "active", "Obnova certifikátu – postup krok za krokem"),
        (3, "active", "NÁKUPNÍ SEZNAM: mléko, chléb, 2× máslo"),
        (4, "archived", "Příliš žluťoučký kůň úpěl ďábelské ódy"),
        (5, "active", "Konfigurace nginx.conf pro server01 a server-02"),
        (6, "trashed", "Staré heslo k Wi-Fi (necertifikované zařízení)"),
        (7, "active", "Porada: Q3 plán, 15:00, zasedačka B"),
        (8, "active", "Dekompozice: é a é jsou stejné písmeno"),
        (9, "active", "ŘÍJEN říjen Říjen"),
        (10, "archived", "Poznámka bez shody"),
    ];

    private static IEnumerable<TestCaseData> Queries()
    {
        yield return Query("certifikat", null, 1, 2);
        yield return Query("certif", null, 1, 2);
        yield return Query("Certifikátu", null, 2);
        yield return Query("CERTIFIKÁT serveru", null, 1);
        yield return Query("zlutoucky kun", null, 4);
        yield return Query("žluť", "archived", 4);
        yield return Query("žluť", "active");
        yield return Query("server", null, 1, 5);
        yield return Query("server01", null, 5);
        yield return Query("02", null, 5);
        yield return Query("nginx conf", null, 5);
        yield return Query("maslo 2", null, 3);
        yield return Query("wi fi", null, 6);
        yield return Query("wi fi", "active");
        yield return Query("necert", "trashed", 6);
        yield return Query("tifikat");
        yield return Query("q3 15", null, 7);
        yield return Query("e", null, 8);
        yield return Query("s", null, 1, 3, 5, 6, 8, 10);
        yield return Query("é stejne", null, 8);
        yield return Query("rijen", null, 9);
        yield return Query("porada zitra");
        yield return Query("   ");
    }

    protected abstract Task<ISearchConformanceTarget> CreateTargetAsync();

    /// <summary>Identifikátor dokumentu fixtury.</summary>
    protected static Guid Id(int number) => new(number, 0x51ea, 0x4000, [0, 0, 0, 0, 0, 0, 0, 2]);

    [TestCaseSource(nameof(Queries))]
    public async Task Search_FixtureQuery_ReturnsExpectedSetEqualToReference(string query, string? state, int[] expected)
    {
        await using var target = await CreateTargetAsync();
        await IndexFixtureAsync(target);

        var result = await target.Adapter.SearchAsync(new SearchRequest(SearchQuery.Parse(query), SearchEntityTypes.Note, state, 0, 100), CancellationToken.None);

        var reference = Documents
            .Where(document => (state is null || document.State == state) && SearchQuery.Parse(query).Matches(document.Text))
            .Select(document => Id(document.Number));
        Assert.That(result.EntityIds, Is.EquivalentTo(expected.Select(Id)), "očekávaná množina");
        Assert.That(result.EntityIds, Is.EquivalentTo(reference), "referenční vyhodnocení Cruma.Search");
        Assert.That(result.TotalCount, Is.EqualTo(expected.Length));
    }

    [Test]
    public async Task Upsert_ChangedText_ReplacesTokens()
    {
        await using var target = await CreateTargetAsync();
        await IndexFixtureAsync(target);

        await target.Adapter.UpsertAsync(SearchIndexEntry.FromText(SearchEntityTypes.Note, Id(1), "active", "Úplně jiný obsah"), CancellationToken.None);
        await target.CommitAsync();

        var old = await target.Adapter.SearchAsync(new SearchRequest(SearchQuery.Parse("serveru"), SearchEntityTypes.Note, null, 0, 100), CancellationToken.None);
        var updated = await target.Adapter.SearchAsync(new SearchRequest(SearchQuery.Parse("uplne jiny"), SearchEntityTypes.Note, null, 0, 100), CancellationToken.None);
        Assert.That(old.EntityIds, Is.Empty);
        Assert.That(updated.EntityIds, Is.EqualTo(new[] { Id(1) }));
    }

    [Test]
    public async Task Remove_Entry_IsNotFound()
    {
        await using var target = await CreateTargetAsync();
        await IndexFixtureAsync(target);

        await target.Adapter.RemoveAsync(SearchEntityTypes.Note, Id(2), CancellationToken.None);
        await target.CommitAsync();

        var result = await target.Adapter.SearchAsync(new SearchRequest(SearchQuery.Parse("certif"), SearchEntityTypes.Note, null, 0, 100), CancellationToken.None);
        Assert.That(result.EntityIds, Is.EqualTo(new[] { Id(1) }));
    }

    [Test]
    public async Task Search_Paging_ReturnsDisjointPagesWithTotal()
    {
        await using var target = await CreateTargetAsync();
        await IndexFixtureAsync(target);
        var query = SearchQuery.Parse("s");

        var first = await target.Adapter.SearchAsync(new SearchRequest(query, SearchEntityTypes.Note, null, 0, 4), CancellationToken.None);
        var second = await target.Adapter.SearchAsync(new SearchRequest(query, SearchEntityTypes.Note, null, 4, 4), CancellationToken.None);

        Assert.That(first.TotalCount, Is.EqualTo(6));
        Assert.That(first.EntityIds.Concat(second.EntityIds).ToList(), Is.Unique.And.Count.EqualTo(6));
    }

    private static async Task IndexFixtureAsync(ISearchConformanceTarget target)
    {
        foreach (var document in Documents)
        {
            await target.Adapter.UpsertAsync(SearchIndexEntry.FromText(SearchEntityTypes.Note, Id(document.Number), document.State, document.Text), CancellationToken.None);
        }

        await target.CommitAsync();
    }

    private static TestCaseData Query(string query, string? state = null, params int[] expected) =>
        new TestCaseData(query, state, expected).SetArgDisplayNames($"\"{query}\"", state ?? "*");
}
