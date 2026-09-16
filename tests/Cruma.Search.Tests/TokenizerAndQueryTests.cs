namespace Cruma.Search.Tests;

public class TokenizerTests
{
    [Test]
    public void TokenizeText_PunctuationAndWhitespace_SplitsIntoTokens()
    {
        var tokens = Tokenizer.TokenizeText("Nasazení: server-01, (verze 2.4) a ještě  e-mail@example.org!");

        Assert.That(tokens, Is.EqualTo(new[] { "nasazeni", "server", "01", "verze", "2", "4", "a", "jeste", "e", "mail", "example", "org" }));
    }

    [Test]
    public void TokenizeText_ShortWords_AreKeptWithoutStopWords()
    {
        Assert.That(Tokenizer.TokenizeText("a i o v"), Is.EqualTo(new[] { "a", "i", "o", "v" }));
    }

    [Test]
    public void TokenizeText_InflectedWords_AreNotStemmed()
    {
        Assert.That(Tokenizer.TokenizeText("certifikátu certifikáty"), Is.EqualTo(new[] { "certifikatu", "certifikaty" }));
    }

    [Test]
    public void TokenizeText_OnlySeparators_ReturnsEmpty()
    {
        Assert.That(Tokenizer.TokenizeText(" ,.;-– "), Is.Empty);
    }

    [Test]
    public void TokenizeText_LettersOutsideBmp_StayInToken()
    {
        Assert.That(Tokenizer.TokenizeText("x\U0001D400y z"), Is.EqualTo(new[] { "x\U0001D400y", "z" }));
    }

    [Test]
    public void ToIndexString_Tokens_JoinedBySingleSpace()
    {
        Assert.That(Tokenizer.ToIndexString(Tokenizer.TokenizeText("  Dva   tokeny ")), Is.EqualTo("dva tokeny"));
    }
}

public class SearchQueryTests
{
    [Test]
    public void Matches_QueryWithoutDiacritics_FindsTextWithDiacritics()
    {
        Assert.That(SearchQuery.Parse("certifikat").Matches("Certifikát serveru"), Is.True);
    }

    [Test]
    public void Matches_PrefixOfWord_FindsInflectedWord()
    {
        Assert.That(SearchQuery.Parse("certif").Matches("platnost certifikátu vypršela"), Is.True);
    }

    [Test]
    public void Matches_TokenInsideWord_IsNotPrefixMatch()
    {
        Assert.That(SearchQuery.Parse("tifik").Matches("certifikát"), Is.False);
    }

    [Test]
    public void Matches_MultipleTokens_AllMustMatch()
    {
        var query = SearchQuery.Parse("Cert SERV");

        Assert.That(query.Matches("certifikát serveru"), Is.True);
        Assert.That(query.Matches("certifikát klienta"), Is.False);
    }

    [TestCase("")]
    [TestCase(" -- ")]
    public void Matches_EmptyQuery_FindsNothing(string query)
    {
        Assert.That(SearchQuery.Parse(query).IsEmpty, Is.True);
        Assert.That(SearchQuery.Parse(query).Matches("cokoli"), Is.False);
    }

    [Test]
    public void Parse_DuplicateTokens_AreDeduplicated()
    {
        Assert.That(SearchQuery.Parse("Síť síť SIT").Tokens, Is.EqualTo(new[] { "sit" }));
    }
}
