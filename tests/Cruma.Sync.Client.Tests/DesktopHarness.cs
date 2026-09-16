using Cruma.Desktop.Storage;
using Cruma.Server.Tests;
using Cruma.Sync;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cruma.Sync.Client.Tests;

/// <summary>Databáze v kontejneru pro testy této sestavy (sdílená infrastruktura ze serverových testů).</summary>
[SetUpFixture]
public sealed class SyncClientTestSetup
{
    private readonly TestDatabase database = new();

    [OneTimeSetUp]
    public Task StartAsync() => database.StartAsync();

    [OneTimeTearDown]
    public Task StopAsync() => database.StopAsync();
}

/// <summary>Desktop pod testem: lokální SQLite v dočasné složce a synchronizační engine proti testovacímu serveru.</summary>
public sealed class DesktopHarness : IAsyncDisposable
{
    private static int counter;

    private DesktopHarness(string folder, LocalNotesStore store, SyncEngine engine, FlakyTransport transport)
    {
        Folder = folder;
        Store = store;
        Engine = engine;
        Transport = transport;
    }

    public string Folder { get; }

    public LocalNotesStore Store { get; }

    public SyncEngine Engine { get; }

    public FlakyTransport Transport { get; }

    public static async Task<DesktopHarness> CreateAsync(CrumaServerFactory server, Guid userId, string appVersion = "1.0.0")
    {
        var folder = Path.Combine(Path.GetTempPath(), $"cruma-desktop-{Environment.ProcessId}-{Interlocked.Increment(ref counter)}");
        var database = await LocalDatabase.OpenAsync(folder, userId, TimeProvider.System, NullLogger.Instance);
        var store = new LocalNotesStore(database, TimeProvider.System, new LocalStoreOptions());

        var http = server.CreateClientFor(userId).Http;
        var options = new SyncClientOptions { AppVersion = appVersion, ClientInstanceId = new Guid(Interlocked.Increment(ref counter), 0x2e5c, 0x4000, [0, 0, 0, 0, 0, 0, 0, 6]) };
        var transport = new FlakyTransport(new HttpSyncTransport(http, options));
        var engine = new SyncEngine(store, transport, options, TimeProvider.System, NullLogger<SyncEngine>.Instance);
        return new DesktopHarness(folder, store, engine, transport);
    }

    public async ValueTask DisposeAsync()
    {
        await Engine.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(Folder, recursive: true);
    }
}

/// <summary>Přenos, který umí simulovat výpadek: požadavek na server dojde, ale odpověď se ztratí.</summary>
public sealed class FlakyTransport(ISyncTransport inner) : ISyncTransport
{
    public bool LoseNextPushResponse { get; set; }

    public bool Offline { get; set; }

    public int PushCalls { get; private set; }

    public Task<TransportResult<HandshakeResponse>> HandshakeAsync(HandshakeRequest request, CancellationToken cancellationToken) =>
        Offline ? Task.FromResult(new TransportResult<HandshakeResponse>(null, TransportFailure.Network)) : inner.HandshakeAsync(request, cancellationToken);

    public async Task<TransportResult<PushResponse>> PushAsync(PushRequest request, CancellationToken cancellationToken)
    {
        PushCalls++;
        var result = await inner.PushAsync(request, cancellationToken);
        if (LoseNextPushResponse)
        {
            LoseNextPushResponse = false;
            return new TransportResult<PushResponse>(null, TransportFailure.Network);
        }

        return result;
    }

    public Task<TransportResult<PullResponse>> PullAsync(PullRequest request, CancellationToken cancellationToken) => inner.PullAsync(request, cancellationToken);
}
