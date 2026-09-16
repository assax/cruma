namespace Cruma.Sync;

/// <summary>
/// Posouzení kompatibility klienta při handshaku (SYN-001, FR-37): klient pod minimální verzí aplikace nebo
/// s nepodporovanou verzí protokolu je odmítnut kódem <see cref="SyncProtocol.ClientVersionUnsupported"/>.
/// </summary>
public static class VersionCompatibility
{
    /// <param name="request">Handshake klienta.</param>
    /// <param name="minimumAppVersion">Minimální podporovaná verze aplikace (konfigurace serveru, plan.md N-6).</param>
    /// <param name="minimumProtocolVersion">Nejstarší verze protokolu, kterou server ještě obslouží.</param>
    public static HandshakeResponse Evaluate(HandshakeRequest request, Version minimumAppVersion, int minimumProtocolVersion = SyncProtocol.CurrentVersion)
    {
        var supported = Version.TryParse(StripSuffix(request.AppVersion), out var appVersion)
            && appVersion >= minimumAppVersion
            && request.ProtocolVersion >= minimumProtocolVersion
            && request.ProtocolVersion <= SyncProtocol.CurrentVersion;

        return supported
            ? new HandshakeResponse(HandshakeStatus.Accepted, SyncProtocol.CurrentVersion, minimumAppVersion.ToString())
            : new HandshakeResponse(HandshakeStatus.Rejected, SyncProtocol.CurrentVersion, minimumAppVersion.ToString(), SyncProtocol.ClientVersionUnsupported);
    }

    // Verze sestavy může nést příponu SemVer ("1.4.0-beta.2", "1.4.0+abc"); porovnává se jen číselná část.
    private static string StripSuffix(string version)
    {
        var end = version.IndexOfAny(['-', '+']);
        return end < 0 ? version : version[..end];
    }
}
