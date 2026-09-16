using Cruma.Content;

namespace Cruma.Versioning.Tests;

/// <summary>
/// Tabulkové scénáře merge dokumentu (testing-strategy.md §3; TST-001). Číslo scénáře odpovídá seznamu ve
/// strategii; řádky navíc pokrývají zbytek tabulky versioning-and-sync-pattern.md §3.2.
/// </summary>
public class DocumentMergeTests
{
    private static IEnumerable<TestCaseData> Scenarios()
    {
        // (base, current, incoming, expected, conflicts)
        yield return Scenario("1 změny různých bloků se sloučí (FR-28 akc. 1)",
            "a:A b:B", "a:A1 b:B", "a:A b:B2", "a:A1 b:B2", "");
        yield return Scenario("2 změna téhož bloku na obou stranách vytvoří konflikt s oběma variantami (FR-28 akc. 2)",
            "a:A b:B", "a:A1 b:B", "a:A2 b:B", "a:!A1|A2 b:B", "a");
        yield return Scenario("4a úprava proti smazání – server smazal, klient upravil",
            "a:A b:B c:C", "a:A c:C", "a:A b:B2 c:C", "a:A b:B2 c:C", "");
        yield return Scenario("4b úprava proti smazání – server upravil, klient smazal",
            "a:A b:B c:C", "a:A b:B1 c:C", "a:A c:C", "a:A b:B1 c:C", "");
        yield return Scenario("5 stejná změna na obou stranách nevytvoří konflikt",
            "a:A b:B", "a:X b:B", "a:X b:B", "a:X b:B", "");
        yield return Scenario("6a přeuspořádání na serveru a úprava u klienta – obojí zachováno",
            "a:A b:B c:C", "c:C a:A b:B", "a:A b:B2 c:C", "c:C a:A b:B2", "");
        yield return Scenario("6b přeuspořádání u klienta a úprava na serveru – obojí zachováno",
            "a:A b:B c:C", "a:A1 b:B c:C", "b:B c:C a:A", "b:B c:C a:A1", "");
        yield return Scenario("7 blok přidaný na obou stranách na stejné místo – oba, deterministicky server první",
            "a:A b:B", "a:A x:X b:B", "a:A y:Y b:B", "a:A x:X y:Y b:B", "");
        yield return Scenario("10 neznámý typ bloku přežije merge",
            "a:A u:?nové", "a:A1 u:?nové", "a:A u:?nové z:Z", "a:A1 u:?nové z:Z", "");
        yield return Scenario("10b neznámý typ bloku změněný na jedné straně se převezme",
            "a:A u:?nové", "a:A u:?nové", "a:A u:?novější", "a:A u:?novější", "");
        yield return Scenario("smazání na obou stranách blok odstraní",
            "a:A b:B", "a:A", "a:A", "a:A", "");
        yield return Scenario("smazání na jedné straně bez úpravy na druhé blok odstraní",
            "a:A b:B", "a:A b:B", "a:A", "a:A", "");
        yield return Scenario("přidání bloku na začátek u klienta",
            "a:A b:B", "a:A b:B", "n:N a:A b:B", "n:N a:A b:B", "");
        yield return Scenario("přeuspořádání na obou stranách – pořadí serveru",
            "a:A b:B c:C", "c:C b:B a:A", "b:B a:A c:C", "c:C b:B a:A", "");
        yield return Scenario("konflikt a sloučení v jednom dokumentu",
            "a:A b:B c:C", "a:A1 b:B1 c:C", "a:A2 b:B c:C2", "a:!A1|A2 b:B1 c:C2", "a");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void Merge_Scenario_ProducesExpectedDocument(string baseDoc, string current, string incoming, string expected, string conflicts)
    {
        var result = DocumentMerge.Merge(Docs.Parse(baseDoc), Docs.Parse(current), Docs.Parse(incoming), Docs.CurrentOrigin, Docs.IncomingOrigin);

        Assert.That(Docs.Format(result.Document), Is.EqualTo(expected));
        Assert.That(result.ConflictBlockIds, Is.EqualTo(conflicts.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
    }

    [Test]
    public void Merge_ConflictVariants_CarryOriginOfBothSides()
    {
        var result = DocumentMerge.Merge(Docs.Parse("a:A"), Docs.Parse("a:A1"), Docs.Parse("a:A2"), Docs.CurrentOrigin, Docs.IncomingOrigin);

        var conflict = ConflictBlock.TryRead(result.Document.Blocks[0])!;
        Assert.That(conflict.CurrentOrigin, Is.EqualTo(Docs.CurrentOrigin));
        Assert.That(conflict.IncomingOrigin, Is.EqualTo(Docs.IncomingOrigin));
        Assert.That(result.HasConflict, Is.True);
    }

    [Test]
    public void Merge_EditAgainstDeletion_MarksBlocksForUser()
    {
        var restored = DocumentMerge.Merge(Docs.Parse("a:A b:B"), Docs.Parse("a:A"), Docs.Parse("a:A b:B2"), Docs.CurrentOrigin, Docs.IncomingOrigin);
        var kept = DocumentMerge.Merge(Docs.Parse("a:A b:B"), Docs.Parse("a:A b:B1"), Docs.Parse("a:A"), Docs.CurrentOrigin, Docs.IncomingOrigin);

        Assert.That(restored.Notices, Is.EqualTo(new[] { new MergeNotice(MergeNoticeKind.BlockRestoredAfterDeletion, "b") }));
        Assert.That(kept.Notices, Is.EqualTo(new[] { new MergeNotice(MergeNoticeKind.BlockKeptAfterDeletion, "b") }));
    }

    [Test]
    public void Merge_SameInputs_IsDeterministic()
    {
        var first = DocumentMerge.Merge(Docs.Parse("a:A b:B"), Docs.Parse("a:A x:X b:B1"), Docs.Parse("y:Y a:A2 b:B"), Docs.CurrentOrigin, Docs.IncomingOrigin);
        var second = DocumentMerge.Merge(Docs.Parse("a:A b:B"), Docs.Parse("a:A x:X b:B1"), Docs.Parse("y:Y a:A2 b:B"), Docs.CurrentOrigin, Docs.IncomingOrigin);

        Assert.That(first.Document.ContentEquals(second.Document), Is.True);
    }

    [Test]
    public void Merge_UnknownBlock_IsPreservedByteForByte()
    {
        var baseDoc = Docs.Parse("a:A u:?nové");
        var unknown = baseDoc.FindBlock("u")!;

        var result = DocumentMerge.Merge(baseDoc, Docs.Parse("a:A1 u:?nové"), Docs.Parse("a:A u:?nové"), Docs.CurrentOrigin, Docs.IncomingOrigin);

        Assert.That(result.Document.FindBlock("u")!.ToString(), Is.EqualTo(unknown.ToString()));
    }

    private static TestCaseData Scenario(string name, string baseDoc, string current, string incoming, string expected, string conflicts) =>
        new TestCaseData(baseDoc, current, incoming, expected, conflicts).SetName(name);
}
