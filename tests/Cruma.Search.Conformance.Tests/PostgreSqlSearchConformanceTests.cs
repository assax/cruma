using Cruma.Search;
using Cruma.Server.Infrastructure;
using Cruma.Server.Search;
using Cruma.Server.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Cruma.Search.Conformance.Tests;

/// <summary>Konformní sada nad adaptérem PostgreSQL (T-29) a omezení na uživatele uvnitř adaptéru (SRC-004).</summary>
public sealed class PostgreSqlSearchConformanceTests : SearchConformanceSuite
{
    private static int userCounter;
    private PostgreSqlContainer container = null!;

    [OneTimeSetUp]
    public async Task StartDatabaseAsync()
    {
        container = new PostgreSqlBuilder(PostgreSqlImage.FromCompose()).Build();
        await container.StartAsync();
        await using var db = CreateContext(new FixedUser(null));
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task StopDatabaseAsync() => await container.DisposeAsync();

    protected override Task<ISearchConformanceTarget> CreateTargetAsync() =>
        Task.FromResult<ISearchConformanceTarget>(new Target(CreateContext(new FixedUser(NextUser()))));

    [Test]
    public async Task Search_EntriesOfOtherUser_AreNotReturned()
    {
        await using var alice = new Target(CreateContext(new FixedUser(NextUser())));
        await using var bob = new Target(CreateContext(new FixedUser(NextUser())));
        await alice.Adapter.UpsertAsync(SearchIndexEntry.FromText(SearchEntityTypes.Note, Id(100), "active", "Alicin certifikát"), CancellationToken.None);
        await alice.CommitAsync();

        var found = await bob.Adapter.SearchAsync(new SearchRequest(SearchQuery.Parse("certifikat"), SearchEntityTypes.Note, null, 0, 10), CancellationToken.None);

        Assert.That(found.EntityIds, Is.Empty);
        Assert.That(found.TotalCount, Is.Zero);
    }

    private CrumaDbContext CreateContext(ICurrentUser user)
    {
        var options = new DbContextOptionsBuilder<CrumaDbContext>();
        InfrastructureModule.ConfigureDbContext(options, container.GetConnectionString());
        return new CrumaDbContext(options.Options, user);
    }

    private static Guid NextUser() => new(Interlocked.Increment(ref userCounter), 0x05e7, 0x4000, [0, 0, 0, 0, 0, 0, 0, 3]);

    private sealed class Target(CrumaDbContext db) : ISearchConformanceTarget
    {
        public ISearchIndexAdapter Adapter { get; } = new PostgreSqlSearchIndexAdapter(db, TimeProvider.System);

        public Task CommitAsync() => db.SaveChangesAsync();

        public ValueTask DisposeAsync() => db.DisposeAsync();
    }

    private sealed class FixedUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId => userId;
    }
}
