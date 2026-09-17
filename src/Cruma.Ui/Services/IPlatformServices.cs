namespace Cruma.Ui.Services;

/// <summary>
/// Popis schopností shellu (UI-003, ui-pattern.md §2). Komponenty podle něj funkce zobrazují, skrývají nebo
/// zakazují – nikdy nezjišťují typ platformy.
/// </summary>
public interface ICapabilities
{
    /// <summary>Plná práce bez připojení (desktop).</summary>
    bool WorksOffline { get; }

    /// <summary>Režimy šířky editoru standardní / rozšířená / plná (desktop, FR-8).</summary>
    bool EditorWidthModes { get; }

    /// <summary>Bez připojení jde vytvořit novou poznámku do fronty (tenký klient, FR-30).</summary>
    bool QueuesNewNotesOffline { get; }

    bool ConflictResolution { get; }
}

/// <summary>Stav připojení k serveru (FR-29 akc. 4).</summary>
public interface IConnectivity
{
    bool IsOnline { get; }

    event Action? Changed;
}

/// <summary>Uživatelské předvolby zařízení (motiv, šířka editoru) – ne doménová data (UI-004).</summary>
public interface IPreferences
{
    ValueTask<string?> GetAsync(string key);

    ValueTask SetAsync(string key, string value);
}

/// <summary>Stav synchronizace pro zobrazení (SYN-007); <c>null</c>, pokud shell synchronizaci nemá (tenký klient).</summary>
public sealed record SyncIndicator(string State, string Label, string? Detail);

public interface ISyncStatusView
{
    SyncIndicator? Current { get; }

    event Action? Changed;

    /// <summary>Spustí synchronizaci hned (tlačítko v UI).</summary>
    void SyncNow();
}

/// <summary>Dostupná aktualizace aplikace (FR-37 akc. 4).</summary>
public sealed record AppUpdateInfo(string Version);

/// <summary>Aktualizace aplikace; jen shelly, které se instalují (desktop).</summary>
public interface IAppUpdates
{
    bool Supported { get; }

    string CurrentVersion { get; }

    Task<AppUpdateInfo?> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>Stáhne a nainstaluje aktualizaci bez práv administrátora a restartuje aplikaci.</summary>
    Task ApplyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Oznámení, že se data změnila mimo aktuální stránku (synchronizace, jiné zařízení); seznamy a detail se znovu načtou.
/// </summary>
public interface IDataChanges
{
    event Action? Changed;

    void NotifyChanged();
}

/// <summary>Výchozí implementace – jedna instance pro celou aplikaci.</summary>
public sealed class DataChanges : IDataChanges
{
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}

/// <summary>Nové poznámky čekající na odeslání (FR-30 akc. 2).</summary>
public interface IPendingNotes
{
    IReadOnlyList<PendingNote> Items { get; }

    event Action? Changed;
}
