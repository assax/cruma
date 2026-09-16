using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Cruma.Api.Contracts;
using Cruma.Desktop.Platform;
using Cruma.Desktop.Storage;
using Cruma.Sync.Client;
using Microsoft.Extensions.Logging;

namespace Cruma.Desktop;

/// <summary>
/// Běh desktopu pro přihlášeného uživatele: lokální databáze, zápisová cesta a synchronizace. Aplikace se spustí
/// a zobrazí data bez sítě i s vypršelým přihlášením (FR-26 akc. 2); první spuštění vyžaduje přihlášení a stažení dat
/// ze serveru (FR-26 akc. 3, I1-D-3).
/// </summary>
public sealed class DesktopRuntime(
    CrumaAppData appData,
    DesktopOptions options,
    ProtectedTokenStore tokens,
    TimeProvider time,
    ILoggerFactory loggerFactory) : IAsyncDisposable
{
    private readonly ILogger<DesktopRuntime> logger = loggerFactory.CreateLogger<DesktopRuntime>();
    private readonly CookieContainer cookies = new();
    private HttpClient? http;

    public Guid? UserId { get; private set; }

    public LocalNotesStore? Store { get; private set; }

    public SyncEngine? Engine { get; private set; }

    public event Action? SessionChanged;

    public event Action<SyncStatus>? SyncStatusChanged;

    private string SessionFile => Path.Combine(appData.Root, "session.json");

    /// <summary>Obnoví relaci z posledního přihlášení; bez sítě pracuje lokálně.</summary>
    public async Task StartAsync()
    {
        if (!File.Exists(SessionFile))
        {
            return;
        }

        var session = JsonSerializer.Deserialize<StoredSession>(await File.ReadAllTextAsync(SessionFile));
        if (session is null)
        {
            return;
        }

        RestoreCookie();
        await OpenAsync(session.UserId);
    }

    /// <summary>Vývojové přihlášení přes server (jen Debug build, viz <see cref="DesktopOptions.DevelopmentSignIn"/>).</summary>
    public async Task<Guid?> SignInDevelopmentAsync(string subject)
    {
        if (!options.DevelopmentSignIn)
        {
            return null;
        }

        HttpResponseMessage response;
        try
        {
            response = await Http.PostAsJsonAsync("auth/dev/sign-in", new DevelopmentSignInRequest(subject));
        }
        catch (HttpRequestException exception)
        {
            // První přihlášení vyžaduje připojení (FR-26 akc. 3).
            logger.LogWarning(exception, "Sign-in failed: server unreachable");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Sign-in failed with status {Status}", (int)response.StatusCode);
            return null;
        }

        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        StoreCookie();
        await File.WriteAllTextAsync(SessionFile, JsonSerializer.Serialize(new StoredSession(user!.UserId)));
        await OpenAsync(user.UserId);

        // První synchronizace stáhne výchozí kategorii a data uživatele (I1-D-3).
        if (Engine is not null)
        {
            await Engine.SynchronizeAsync();
        }

        return user.UserId;
    }

    /// <summary>Odhlášení (FR-32 akc. 5): smaže token a relaci; lokální databáze zůstává pro další přihlášení.</summary>
    public async Task SignOutAsync()
    {
        try
        {
            await Http.PostAsync("auth/sign-out", null);
        }
        catch (HttpRequestException exception)
        {
            // Bez sítě se odhlásí jen lokálně; session na serveru vyprší sama.
            logger.LogInformation(exception, "Server sign-out skipped: server unreachable");
        }

        tokens.Clear();
        File.Delete(SessionFile);
        await CloseAsync();
        UserId = null;
        SessionChanged?.Invoke();
    }

    private HttpClient Http => http ??= new HttpClient(new HttpClientHandler { CookieContainer = cookies, UseCookies = true })
    {
        BaseAddress = new Uri(options.ServerUrl),
        Timeout = TimeSpan.FromSeconds(30),
    };

    private async Task OpenAsync(Guid userId)
    {
        await CloseAsync();
        var database = await LocalDatabase.OpenAsync(appData.Root, userId, time, logger);
        Store = new LocalNotesStore(database, time, new LocalStoreOptions());

        var syncOptions = new SyncClientOptions
        {
            AppVersion = typeof(DesktopRuntime).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            ClientInstanceId = ClientInstanceId(),
        };
        Engine = new SyncEngine(Store, new HttpSyncTransport(Http, syncOptions), syncOptions, time, loggerFactory.CreateLogger<SyncEngine>());
        Engine.StatusChanged += status => SyncStatusChanged?.Invoke(status);
        Engine.Start();

        UserId = userId;
        SessionChanged?.Invoke();
        logger.LogInformation("Desktop session opened for user {UserId}", userId);
    }

    private async Task CloseAsync()
    {
        if (Engine is not null)
        {
            await Engine.DisposeAsync();
        }

        Engine = null;
        Store = null;
    }

    // Identifikátor instance desktopu je stálý pro instalaci – server podle něj spojuje uložení do verzí (N-3).
    private Guid ClientInstanceId()
    {
        var file = Path.Combine(appData.Root, "client-instance.id");
        if (File.Exists(file) && Guid.TryParse(File.ReadAllText(file), out var existing))
        {
            return existing;
        }

        var created = Guid.CreateVersion7(time.GetUtcNow());
        File.WriteAllText(file, created.ToString());
        return created;
    }

    private void StoreCookie() =>
        tokens.Save(cookies.GetCookieHeader(new Uri(options.ServerUrl)));

    private void RestoreCookie()
    {
        if (tokens.Load() is { Length: > 0 } header)
        {
            cookies.SetCookies(new Uri(options.ServerUrl), header.Replace("; ", ","));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        http?.Dispose();
    }

    private sealed record StoredSession(Guid UserId);
}
