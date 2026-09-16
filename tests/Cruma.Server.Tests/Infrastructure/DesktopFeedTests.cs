using System.Net;
using Cruma.Server.Infrastructure;

namespace Cruma.Server.Tests.Infrastructure;

/// <summary>Feed instalátoru a aktualizací desktopu na serveru bez přihlášení (T-53, I1-D-5, plan.md N-6).</summary>
public class DesktopFeedTests
{
    [Test]
    public async Task Feed_ConfiguredFolder_IsServedAnonymously()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"cruma-feed-{Environment.ProcessId}");
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "releases.win.json"), """{"Assets":[]}""");
        await File.WriteAllBytesAsync(Path.Combine(folder, "Cruma-0.1.0-full.nupkg"), [1, 2, 3]);
        try
        {
            using var server = new CrumaServerFactory().WithSetting(DesktopFeed.PathKey, folder);
            var anonymous = server.CreateClient();

            var releases = await anonymous.GetAsync("/desktop/releases.win.json");
            var package = await anonymous.GetAsync("/desktop/Cruma-0.1.0-full.nupkg");
            var missing = await anonymous.GetAsync("/desktop/neexistuje.exe");

            Assert.That(releases.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(await package.Content.ReadAsByteArrayAsync(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(missing.StatusCode, Is.Not.EqualTo(HttpStatusCode.OK));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
