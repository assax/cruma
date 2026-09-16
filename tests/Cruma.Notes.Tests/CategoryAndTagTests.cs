namespace Cruma.Notes.Tests;

public class CategoryTests
{
    private static readonly Category DefaultCategory = Category.CreateDefault(Guid.Parse("00000000-0000-0000-0000-0000000000d1"), "Poznámky");
    private static readonly Category Work = Category.Create(Guid.Parse("00000000-0000-0000-0000-0000000000c1"), "Práce");

    [Test]
    public void Rename_NewName_IsTrimmed()
    {
        Assert.That(Work.Rename("  Projekty ").Name, Is.EqualTo("Projekty"));
    }

    [Test]
    public void Create_BlankName_Throws()
    {
        var exception = Assert.Throws<NotesRuleException>(() => Category.Create(Guid.Parse("00000000-0000-0000-0000-0000000000e1"), " "));
        Assert.That(exception.Code, Is.EqualTo(NotesErrorCodes.NameRequired));
    }

    [Test]
    public void Delete_CategoryWithNotes_MovesNotesToDefaultCategory()
    {
        var inWork = Note.Create(Guid.Parse("00000000-0000-0000-0000-000000000001"), DefaultCategory).MoveToCategory(Work.Id).WithTitle("a");
        var elsewhere = Note.Create(Guid.Parse("00000000-0000-0000-0000-000000000002"), DefaultCategory);

        var moved = Work.Delete(DefaultCategory, [inWork, elsewhere]);

        Assert.That(moved, Has.Count.EqualTo(1));
        Assert.That(moved[0].Id, Is.EqualTo(inWork.Id));
        Assert.That(moved[0].CategoryId, Is.EqualTo(DefaultCategory.Id));
        Assert.That(moved[0].Title, Is.EqualTo("a"));
    }

    [Test]
    public void Delete_DefaultCategory_Throws()
    {
        var exception = Assert.Throws<NotesRuleException>(() => DefaultCategory.Delete(DefaultCategory, []));
        Assert.That(exception.Code, Is.EqualTo(NotesErrorCodes.DefaultCategoryCannotBeDeleted));
    }

    [Test]
    public void Delete_TargetIsNotDefault_Throws()
    {
        var other = Category.Create(Guid.Parse("00000000-0000-0000-0000-0000000000e1"), "Jiná");

        Assert.Throws<NotesRuleException>(() => Work.Delete(other, []));
    }
}

public class TagTests
{
    private static readonly Category DefaultCategory = Category.CreateDefault(Guid.Parse("00000000-0000-0000-0000-0000000000d1"), "Poznámky");
    private static readonly Tag Important = Tag.Create(Guid.Parse("00000000-0000-0000-0000-0000000000a1"), "důležité");

    [Test]
    public void Delete_Tag_RemovesItFromNotesAndKeepsNotes()
    {
        var tagged = Note.Create(Guid.Parse("00000000-0000-0000-0000-000000000001"), DefaultCategory).AddTag(Important.Id);
        var untagged = Note.Create(Guid.Parse("00000000-0000-0000-0000-000000000002"), DefaultCategory);

        var changed = Important.Delete([tagged, untagged]);

        Assert.That(changed, Has.Count.EqualTo(1));
        Assert.That(changed[0].Id, Is.EqualTo(tagged.Id));
        Assert.That(changed[0].TagIds, Is.Empty);
    }
}

public class NoteViewsTests
{
    private static readonly Category DefaultCategory = Category.CreateDefault(Guid.Parse("00000000-0000-0000-0000-0000000000d1"), "Poznámky");
    private static readonly Guid Work = Guid.Parse("00000000-0000-0000-0000-0000000000c1");
    private static readonly Guid TagId = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    [Test]
    public void Overview_MixedNotes_ShowsActiveWithPinnedFirst()
    {
        var first = NewNote(1);
        var pinned = NewNote(2).Pin();
        var archived = NewNote(3).Archive();
        var trashed = NewNote(4).MoveToTrash();

        var overview = NoteViews.Overview([first, pinned, archived, trashed]);

        Assert.That(overview, Is.EqualTo(new[] { pinned, first }));
    }

    [Test]
    public void ArchiveAndTrash_MixedNotes_ContainOnlyTheirState()
    {
        Note[] notes = [NewNote(1), NewNote(2).Archive(), NewNote(3).MoveToTrash()];

        Assert.That(NoteViews.Archive(notes).Select(note => note.State), Is.EqualTo(new[] { NoteState.Archived }));
        Assert.That(NoteViews.Trash(notes).Select(note => note.State), Is.EqualTo(new[] { NoteState.Trashed }));
    }

    [Test]
    public void WithTag_NotesInDifferentCategories_ReturnsAllTagged()
    {
        var inDefault = NewNote(1).AddTag(TagId);
        var inWork = NewNote(2).MoveToCategory(Work).AddTag(TagId);
        var untagged = NewNote(3);

        Assert.That(NoteViews.WithTag([inDefault, inWork, untagged], TagId), Is.EquivalentTo(new[] { inDefault, inWork }));
    }

    private static Note NewNote(int number) => Note.Create(new Guid(number, 0, 0, new byte[8]), DefaultCategory);
}
