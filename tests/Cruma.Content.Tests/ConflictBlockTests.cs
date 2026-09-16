namespace Cruma.Content.Tests;

public class ConflictBlockTests
{
    [Test]
    public void TryRead_WrittenConflict_ReturnsBothVariantsWithOrigin()
    {
        var conflict = new ConflictBlock(
            "a",
            TestDocuments.Paragraph("a", "server"),
            new ConflictOrigin("web", new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero)),
            TestDocuments.Paragraph("a", "klient"),
            new ConflictOrigin("desktop", new DateTimeOffset(2026, 9, 1, 11, 0, 0, TimeSpan.FromHours(2))));

        var block = conflict.ToBlock();
        var reloaded = ConflictBlock.TryRead(ContentDocument.Parse(ContentDocument.Create([block]).ToJsonString()).Blocks[0])!;

        Assert.That(block.Id, Is.EqualTo("a"));
        Assert.That(block.Type, Is.EqualTo(ContentTypes.Conflict));
        Assert.That(reloaded.Current.ContentEquals(conflict.Current), Is.True);
        Assert.That(reloaded.Incoming.ContentEquals(conflict.Incoming), Is.True);
        Assert.That(reloaded.CurrentOrigin, Is.EqualTo(conflict.CurrentOrigin));
        Assert.That(reloaded.IncomingOrigin.ChangedAtUtc, Is.EqualTo(conflict.IncomingOrigin.ChangedAtUtc));
        Assert.That(reloaded.IncomingOrigin.ClientType, Is.EqualTo("desktop"));
    }

    [Test]
    public void TryRead_OrdinaryBlock_ReturnsNull()
    {
        Assert.That(ConflictBlock.TryRead(TestDocuments.Paragraph("a", "x")), Is.Null);
    }
}

public class LinkPolicyTests
{
    [TestCase("https://example.org", true)]
    [TestCase("http://example.org/a?b=c", true)]
    [TestCase("mailto:someone@example.org", true)]
    [TestCase("HTTPS://EXAMPLE.ORG", true)]
    [TestCase("javascript:alert(1)", false)]
    [TestCase("data:text/html;base64,AAAA", false)]
    [TestCase("file:///c:/windows", false)]
    [TestCase("relative/path", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsAllowed_Scheme_MatchesPolicy(string? href, bool expected)
    {
        Assert.That(LinkPolicy.IsAllowed(href), Is.EqualTo(expected));
    }
}
