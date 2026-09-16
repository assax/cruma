using Cruma.Notes;
using static Cruma.Versioning.Tests.TestNotes;

namespace Cruma.Versioning.Tests;

/// <summary>Scénáře 8 a 9 z testing-strategy.md §3 a pravidla VER-005.</summary>
public class MetadataMergeTests
{
    [Test]
    public void Merge_Scenario8_TagsAddedAndRemovedOnBothSides_AreSetMerged()
    {
        var baseNote = NewNote().AddTag(TagA).AddTag(TagB);
        var current = baseNote.RemoveTag(TagA).AddTag(TagC);
        var incoming = baseNote.RemoveTag(TagB);

        var result = MetadataMerge.Merge(baseNote, current, incoming);

        Assert.That(result.Metadata.TagIds, Is.EquivalentTo(new[] { TagC }));
        Assert.That(result.OverwrittenValues, Is.Empty);
    }

    [Test]
    public void Merge_Scenario9_TitleChangedOnBothSides_LaterReceivedWinsAndOverwrittenIsRecorded()
    {
        var baseNote = NewNote().WithTitle("Původní");
        var current = baseNote.WithTitle("Ze serveru");
        var incoming = baseNote.WithTitle("Z desktopu");

        var result = MetadataMerge.Merge(baseNote, current, incoming);

        Assert.That(result.Metadata.Title, Is.EqualTo("Z desktopu"));
        Assert.That(result.OverwrittenValues, Is.EqualTo(new[] { new OverwrittenValue(MetadataMerge.TitleField, "Ze serveru", "Z desktopu") }));
    }

    [Test]
    public void Merge_ScalarsChangedOnDifferentSides_AreBothTaken()
    {
        var baseNote = NewNote();
        var current = baseNote.MoveToCategory(Work).Pin();
        var incoming = baseNote.WithColor("green").Archive();

        var result = MetadataMerge.Merge(baseNote, current, incoming);

        Assert.That(result.Metadata, Is.EqualTo(baseNote.MoveToCategory(Work).Pin().WithColor("green").Archive()));
        Assert.That(result.OverwrittenValues, Is.Empty);
    }

    [Test]
    public void Merge_AllScalarsConflicting_RecordsEveryOverwrittenField()
    {
        var baseNote = NewNote();
        var current = baseNote.WithTitle("s").MoveToCategory(Work).WithColor("red").Pin().Archive();
        var incoming = baseNote.WithTitle("k").MoveToCategory(Private).WithColor("blue").Pin().MoveToTrash();

        var result = MetadataMerge.Merge(baseNote, current, incoming);

        Assert.That(result.Metadata.Title, Is.EqualTo("k"));
        Assert.That(result.Metadata.CategoryId, Is.EqualTo(Private));
        Assert.That(result.Metadata.State, Is.EqualTo(NoteState.Trashed));
        Assert.That(result.OverwrittenValues.Select(value => value.Field),
            Is.EqualTo(new[] { MetadataMerge.TitleField, MetadataMerge.CategoryField, MetadataMerge.ColorField, MetadataMerge.StateField }),
            "připnutí je na obou stranách stejné, nepřepisuje se");
    }

    [Test]
    public void Merge_TitleClearedOnOneSide_IsCleared()
    {
        var baseNote = NewNote().WithTitle("Název");

        var result = MetadataMerge.Merge(baseNote, baseNote, baseNote.WithTitle(null));

        Assert.That(result.Metadata.Title, Is.Null);
    }
}
