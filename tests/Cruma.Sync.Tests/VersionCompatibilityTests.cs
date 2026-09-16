namespace Cruma.Sync.Tests;

public class VersionCompatibilityTests
{
    private static readonly Guid Instance = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Version Minimum = new(1, 4, 0);

    [TestCase("1.4.0")]
    [TestCase("1.4.1")]
    [TestCase("2.0.0")]
    [TestCase("1.4.0-beta.2")]
    [TestCase("1.5.0+sha.abc")]
    public void Evaluate_AppVersionAtOrAboveMinimum_IsAccepted(string appVersion)
    {
        var response = VersionCompatibility.Evaluate(new HandshakeRequest(appVersion, SyncProtocol.CurrentVersion, Instance), Minimum);

        Assert.That(response.Status, Is.EqualTo(HandshakeStatus.Accepted));
        Assert.That(response.ErrorCode, Is.Null);
    }

    [TestCase("1.3.9")]
    [TestCase("0.9")]
    [TestCase("nesmysl")]
    [TestCase("")]
    public void Evaluate_AppVersionBelowMinimumOrInvalid_IsRejectedWithCode(string appVersion)
    {
        var response = VersionCompatibility.Evaluate(new HandshakeRequest(appVersion, SyncProtocol.CurrentVersion, Instance), Minimum);

        Assert.That(response.Status, Is.EqualTo(HandshakeStatus.Rejected));
        Assert.That(response.ErrorCode, Is.EqualTo(SyncProtocol.ClientVersionUnsupported));
        Assert.That(response.MinimumAppVersion, Is.EqualTo("1.4.0"));
    }

    [TestCase(0)]
    [TestCase(SyncProtocol.CurrentVersion + 1)]
    public void Evaluate_UnsupportedProtocolVersion_IsRejected(int protocolVersion)
    {
        var response = VersionCompatibility.Evaluate(new HandshakeRequest("9.0.0", protocolVersion, Instance), Minimum);

        Assert.That(response.Status, Is.EqualTo(HandshakeStatus.Rejected));
        Assert.That(response.ErrorCode, Is.EqualTo(SyncProtocol.ClientVersionUnsupported));
    }
}
