namespace Cruma.Sync;

/// <summary>
/// Začátek synchronizační relace: klient ohlásí verzi aplikace a protokolu (versioning-and-sync-pattern.md §5.1,
/// SYN-001). <see cref="ClientInstanceId"/> odliší instance klienta téhož uživatele (plan.md N-3).
/// </summary>
public sealed record HandshakeRequest(string AppVersion, int ProtocolVersion, Guid ClientInstanceId);

/// <summary>Výsledek handshaku.</summary>
public enum HandshakeStatus
{
    Accepted,
    Rejected,
}

/// <summary>
/// Odpověď na handshake. Při odmítnutí nese <see cref="ErrorCode"/> <c>client_version_unsupported</c> a minimální
/// verzi; klient pak nesynchronizuje, ponechá všechny lokální změny a vyzve k aktualizaci (FR-37 akc. 1, 2).
/// </summary>
public sealed record HandshakeResponse(
    HandshakeStatus Status,
    int ServerProtocolVersion,
    string MinimumAppVersion,
    string? ErrorCode = null);
