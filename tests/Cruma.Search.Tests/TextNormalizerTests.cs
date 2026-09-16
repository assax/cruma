namespace Cruma.Search.Tests;

public class TextNormalizerTests
{
    [TestCase("Certifikát", "certifikat")]
    [TestCase("PŘÍLIŠ ŽLUŤOUČKÝ KŮŇ ÚPĚL ĎÁBELSKÉ ÓDY", "prilis zlutoucky kun upel dabelske ody")]
    [TestCase("Ärger über Öl", "arger uber ol")]
    [TestCase("ABC123", "abc123")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void Normalize_Text_RemovesCaseAndDiacritics(string? text, string expected)
    {
        Assert.That(TextNormalizer.Normalize(text), Is.EqualTo(expected));
    }

    [Test]
    public void Normalize_DecomposedAndComposedForms_GiveSameResult()
    {
        const string composed = "certifikát";
        const string decomposed = "certifikát";

        Assert.That(TextNormalizer.Normalize(decomposed), Is.EqualTo(TextNormalizer.Normalize(composed)));
        Assert.That(TextNormalizer.Normalize(decomposed), Is.EqualTo("certifikat"));
    }

    [Test]
    [SetCulture("tr-TR")]
    public void Normalize_CapitalIUnderTurkishCulture_IsCultureIndependent()
    {
        Assert.That(TextNormalizer.Normalize("CERTIFIKÁT"), Is.EqualTo("certifikat"));
    }

    [Test]
    public void Version_IsExplicit()
    {
        Assert.That(TextNormalizer.Version, Is.EqualTo(1), "změna normalizace musí zvýšit verzi (SRC-002) a upravit tento test");
    }
}
