namespace Cruma.Notes.Tests;

public class NoteTests
{
    private static readonly Category DefaultCategory = Category.CreateDefault(Guid.Parse("00000000-0000-0000-0000-0000000000d1"), "Poznámky");
    private static readonly Category Work = Category.Create(Guid.Parse("00000000-0000-0000-0000-0000000000c1"), "Práce");
    private static readonly Guid NoteId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TagA = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    [Test]
    public void Create_OnlyIdentifier_IsInDefaultCategoryWithoutOtherProperties()
    {
        var note = Note.Create(NoteId, DefaultCategory);

        Assert.That(note.CategoryId, Is.EqualTo(DefaultCategory.Id));
        Assert.That(note.Title, Is.Null);
        Assert.That(note.TagIds, Is.Empty);
        Assert.That(note.Color, Is.Null);
        Assert.That(note.IsPinned, Is.False);
        Assert.That(note.State, Is.EqualTo(NoteState.Active));
    }

    [Test]
    public void Create_NonDefaultCategory_Throws()
    {
        var exception = Assert.Throws<NotesRuleException>(() => Note.Create(NoteId, Work));
        Assert.That(exception.Code, Is.EqualTo(NotesErrorCodes.DefaultCategoryRequired));
    }

    [Test]
    public void Properties_SetAndCleared_ReturnToOriginal()
    {
        var original = Note.Create(NoteId, DefaultCategory);

        var changed = original.WithTitle("Název").MoveToCategory(Work.Id).AddTag(TagA).WithColor("yellow").Pin();
        var cleared = changed.WithTitle(null).MoveToCategory(DefaultCategory.Id).RemoveTag(TagA).WithColor(null).Unpin();

        Assert.That(changed.Title, Is.EqualTo("Název"));
        Assert.That(changed.CategoryId, Is.EqualTo(Work.Id));
        Assert.That(changed.TagIds, Is.EquivalentTo(new[] { TagA }));
        Assert.That(changed.Color, Is.EqualTo("yellow"));
        Assert.That(changed.IsPinned, Is.True);
        Assert.That(cleared, Is.EqualTo(original));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void WithTitle_Blank_MeansNoTitle(string title)
    {
        Assert.That(Note.Create(NoteId, DefaultCategory).WithTitle(title).Title, Is.Null);
    }

    [TestCase("Yellow")]
    [TestCase("#ffcc00")]
    [TestCase("")]
    public void WithColor_NotAThemeKey_Throws(string color)
    {
        Assert.Throws<NotesRuleException>(() => Note.Create(NoteId, DefaultCategory).WithColor(color));
    }

    [Test]
    public void Archive_ThenUnarchive_ReturnsToActive()
    {
        var archived = Note.Create(NoteId, DefaultCategory).Archive();

        Assert.That(archived.State, Is.EqualTo(NoteState.Archived));
        Assert.That(archived.Unarchive().State, Is.EqualTo(NoteState.Active));
    }

    [Test]
    public void MoveToTrash_FromArchive_CanBeRestoredToActive()
    {
        var trashed = Note.Create(NoteId, DefaultCategory).Archive().MoveToTrash();

        Assert.That(trashed.State, Is.EqualTo(NoteState.Trashed));
        Assert.That(trashed.RestoreFromTrash().State, Is.EqualTo(NoteState.Active));
    }

    [Test]
    public void Archive_TrashedNote_Throws()
    {
        var exception = Assert.Throws<NotesRuleException>(() => Note.Create(NoteId, DefaultCategory).MoveToTrash().Archive());
        Assert.That(exception.Code, Is.EqualTo(NotesErrorCodes.InvalidStateTransition));
    }

    [Test]
    public void EnsureCanBeDeletedPermanently_NoteNotInTrash_Throws()
    {
        var note = Note.Create(NoteId, DefaultCategory);

        var exception = Assert.Throws<NotesRuleException>(note.EnsureCanBeDeletedPermanently);
        Assert.That(exception.Code, Is.EqualTo(NotesErrorCodes.NoteNotInTrash));
        Assert.DoesNotThrow(note.MoveToTrash().EnsureCanBeDeletedPermanently);
    }

    [Test]
    public void Equals_SameTagsInDifferentOrder_AreEqual()
    {
        var tagB = Guid.Parse("00000000-0000-0000-0000-0000000000b1");
        var first = Note.Restore(NoteId, "x", Work.Id, [TagA, tagB], null, false, NoteState.Active);
        var second = Note.Restore(NoteId, "x", Work.Id, [tagB, TagA], null, false, NoteState.Active);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
    }
}
