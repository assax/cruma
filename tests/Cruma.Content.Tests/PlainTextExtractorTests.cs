namespace Cruma.Content.Tests;

public class PlainTextExtractorTests
{
    [Test]
    public void Extract_AllElements_ReturnsTitleAndTextInReadingOrder()
    {
        var document = ContentDocument.Parse(TestDocuments.Fixture("document-v1-all-elements.json"));

        var text = PlainTextExtractor.Extract("Nasazení", document);

        Assert.That(text, Is.EqualTo(
            "Nasazení\n" +
            "Certifikát serveru\n" +
            "tučně kurzíva podtržená přeškrtnuto zvýrazněno odkaz\n" +
            "první\ndruhá\n" +
            "krok\n" +
            "hotovo\nzbývá\n" +
            "z novější verze"));
    }

    [Test]
    public void Extract_NoTitle_ReturnsOnlyContent()
    {
        var document = ContentDocument.Create([TestDocuments.Paragraph("a", "jen obsah")]);

        Assert.That(PlainTextExtractor.Extract(null, document), Is.EqualTo("jen obsah"));
    }

    [Test]
    public void Extract_ConflictBlock_ContainsBothVariants()
    {
        var origin = new ConflictOrigin("desktop", DateTimeOffset.UnixEpoch);
        var conflict = new ConflictBlock("a", TestDocuments.Paragraph("a", "verze serveru"), origin, TestDocuments.Paragraph("a", "verze klienta"), origin);
        var document = ContentDocument.Create([conflict.ToBlock()]);

        Assert.That(PlainTextExtractor.Extract(null, document), Is.EqualTo("verze serveru\nverze klienta"));
    }

    [Test]
    public void Extract_EmptyDocument_ReturnsEmpty()
    {
        Assert.That(PlainTextExtractor.Extract("  ", ContentDocument.Empty), Is.Empty);
    }
}
