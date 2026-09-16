using System.IO;
using System.Net.Http;
using System.Text.Json;
using Cruma.Sync.Client;
using Cruma.Ui.Services;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Cruma.Desktop.Platform;

/// <summary>Schopnosti desktopu (ui-pattern.md §2): plně offline, režimy šířky editoru, řešení konfliktů.</summary>
public sealed class DesktopCapabilities : ICapabilities
{
    public bool WorksOffline => true;

    public bool EditorWidthModes => true;

    public bool QueuesNewNotesOffline => false;

    public bool ConflictResolution => true;
}

/// <summary>Připojení podle výsledku poslední synchronizace – desktop pracuje offline plnohodnotně.</summary>
public sealed class DesktopConnectivity : IConnectivity
{
    public DesktopConnectivity(DesktopRuntime runtime) =>
        runtime.SyncStatusChanged += status =>
        {
            if (IsOnline == status.Offline)
            {
                IsOnline = !status.Offline;
                Changed?.Invoke();
            }
        };

    public bool IsOnline { get; private set; } = true;

    public event Action? Changed;
}

/// <summary>Stav synchronizace v hlavičce aplikace (SYN-007).</summary>
public sealed class DesktopSyncStatusView : ISyncStatusView
{
    private readonly DesktopRuntime runtime;

    public DesktopSyncStatusView(DesktopRuntime runtime)
    {
        this.runtime = runtime;
        runtime.SyncStatusChanged += status =>
        {
            Current = new SyncIndicator(status.State.ToString(), Label(status.State), status.Detail);
            Changed?.Invoke();
        };
    }

    public SyncIndicator? Current { get; private set; } = new("Pending", "Synchronizace…", null);

    public event Action? Changed;

    public void SyncNow() => runtime.Engine?.RequestSync(TimeSpan.Zero);

    private static string Label(SyncState state) => state switch
    {
        SyncState.Synced => "Synchronizováno",
        SyncState.Pending => "Čeká",
        SyncState.Conflict => "Konflikt",
        _ => "Chyba",
    };
}

/// <summary>Desktop frontu nových poznámek nemá – všechny změny čekají v logu změn a ukazuje je stav synchronizace.</summary>
public sealed class NoPendingNotes : IPendingNotes
{
    public IReadOnlyList<PendingNote> Items => [];

    public event Action? Changed
    {
        add { }
        remove { }
    }
}

/// <summary>Předvolby UI v souboru v datové složce (PER-006).</summary>
public sealed class FilePreferences(CrumaAppData appData) : IPreferences
{
    private readonly object sync = new();

    private string FilePath => Path.Combine(appData.Root, "preferences.json");

    public ValueTask<string?> GetAsync(string key)
    {
        lock (sync)
        {
            return ValueTask.FromResult(Read().GetValueOrDefault(key));
        }
    }

    public ValueTask SetAsync(string key, string value)
    {
        lock (sync)
        {
            var values = Read();
            values[key] = value;
            Directory.CreateDirectory(appData.Root);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(values));
        }

        return ValueTask.CompletedTask;
    }

    private Dictionary<string, string> Read() =>
        File.Exists(FilePath) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(FilePath)) ?? [] : [];
}

/// <summary>
/// Aktualizace přes Velopack (desktop-pattern.md §6): instalace pro uživatele bez práv administrátora, feed na serveru
/// Cruma (I1-D-5). Mimo instalovanou aplikaci (vývoj) nejsou aktualizace dostupné.
/// </summary>
public sealed class VelopackUpdates(DesktopOptions options, ILogger<VelopackUpdates> logger) : IAppUpdates
{
    private readonly UpdateManager? manager = CreateManager(options);
    private UpdateInfo? pending;

    public bool Supported => manager is { IsInstalled: true };

    public string CurrentVersion => manager?.CurrentVersion?.ToString() ?? typeof(VelopackUpdates).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public async Task<AppUpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!Supported)
        {
            return null;
        }

        try
        {
            pending = await manager!.CheckForUpdatesAsync();
            return pending is null ? null : new AppUpdateInfo(pending.TargetFullRelease.Version.ToString());
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException)
        {
            // Feed nedostupný (offline) – aktualizace se nabídne příště.
            logger.LogInformation(exception, "Update check skipped: feed unreachable");
            return null;
        }
    }

    public async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        if (!Supported || pending is null)
        {
            return;
        }

        await manager!.DownloadUpdatesAsync(pending);
        manager.ApplyUpdatesAndRestart(pending);
    }

    private static UpdateManager? CreateManager(DesktopOptions options) =>
        string.IsNullOrWhiteSpace(options.UpdateFeedUrl) ? null : new UpdateManager(new SimpleWebSource(options.UpdateFeedUrl));
}
