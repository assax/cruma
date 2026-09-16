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

/// <summary>Nové poznámky čekající na odeslání (FR-30 akc. 2).</summary>
public interface IPendingNotes
{
    IReadOnlyList<PendingNote> Items { get; }

    event Action? Changed;
}
