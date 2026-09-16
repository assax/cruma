using Cruma.Desktop.Storage;
using Cruma.Desktop.Storage.Search;
using Cruma.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cruma.Search.Conformance.Tests;

/// <summary>Konformní sada nad adaptérem SQLite FTS5 (T-48, TST-002) – skutečný soubor v dočasné složce.</summary>
public sealed class SqliteSearchConformanceTests : SearchConformanceSuite
{
    private static int databaseCounter;
    private string folder = null!;

    [OneTimeSetUp]
    public void CreateFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "cruma-conformance-" + Environment.ProcessId);
        Directory.CreateDirectory(folder);
    }

    [OneTimeTearDown]
    public void DeleteFolder()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(folder, recursive: true);
    }

    protected override async Task<ISearchConformanceTarget> CreateTargetAsync()
    {
        var userId = new Guid(Interlocked.Increment(ref databaseCounter), 0x5117, 0x4000, [0, 0, 0, 0, 0, 0, 0, 4]);
        var database = await LocalDatabase.OpenAsync(folder, userId, TimeProvider.System, NullLogger.Instance);
        return new Target(database.CreateContext());
    }

    private sealed class Target(CrumaLocalDbContext db) : ISearchConformanceTarget
    {
        public ISearchIndexAdapter Adapter { get; } = new SqliteSearchIndexAdapter(db);

        // Adaptér SQLite zapisuje příkazy hned, potvrzení jednotky práce nepotřebuje.
        public Task CommitAsync() => Task.CompletedTask;

        public ValueTask DisposeAsync() => db.DisposeAsync();
    }
}
