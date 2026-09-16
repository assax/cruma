namespace Cruma.Sync;

/// <summary>Verze synchronizačního protokolu a kódy chyb protokolu (NFR-18, SYN-001).</summary>
public static class SyncProtocol
{
    /// <summary>Verze protokolu, kterou mluví tato sestava. Nekompatibilní změna kontraktů ji zvyšuje.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Klient je pod minimální podporovanou verzí aplikace nebo protokolu (FR-37 akc. 1).</summary>
    public const string ClientVersionUnsupported = "client_version_unsupported";
}
