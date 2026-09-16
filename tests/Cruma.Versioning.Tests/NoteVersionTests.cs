using Cruma.Content;
using static Cruma.Versioning.Tests.TestNotes;

namespace Cruma.Versioning.Tests;

public class NoteVersionTests
{
    [Test]
    public void First_NewNote_HasNumberOneWithoutBase()
    {
        var version = NoteVersion.First(NewNote(), Docs.Parse("a:A"), VersionSource.Desktop, T0);

        Assert.That(version.Number, Is.EqualTo(1));
        Assert.That(version.BaseNumber, Is.Null);
        Assert.That(version.Source, Is.EqualTo(VersionSource.Desktop));
        Assert.That(version.CreatedAtUtc, Is.EqualTo(T0));
        Assert.That(version.NoteId, Is.EqualTo(NoteId));
    }

    [Test]
    public void Next_AfterVersion_IncrementsNumberAndKeepsPrevious()
    {
        var first = NoteVersion.First(NewNote(), Docs.Parse("a:A"), VersionSource.Desktop, T0);

        var second = first.Next(Docs.Parse("a:A1"), NewNote(), VersionSource.Web, T0.AddMinutes(10));

        Assert.That(second.Number, Is.EqualTo(2));
        Assert.That(second.BaseNumber, Is.EqualTo(1));
        Assert.That(second.Source, Is.EqualTo(VersionSource.Web));
        Assert.That(Docs.Format(first.Document), Is.EqualTo("a:A"));
    }

    [Test]
    public void Next_CreatedAtWithOffset_IsStoredInUtc()
    {
        var local = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.FromHours(2));

        var version = NoteVersion.First(NewNote(), ContentDocument.Empty, VersionSource.Web, local);

        Assert.That(version.CreatedAtUtc.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(version.CreatedAtUtc, Is.EqualTo(local));
    }

    [Test]
    public void Next_OtherNote_Throws()
    {
        var first = NoteVersion.First(NewNote(), ContentDocument.Empty, VersionSource.Web, T0);
        var other = Notes.Note.Create(Guid.Parse("00000000-0000-0000-0000-000000000099"), DefaultCategory);

        Assert.Throws<ArgumentException>(() => first.Next(ContentDocument.Empty, other, VersionSource.Web, T0));
    }

    [Test]
    public void RestoreFrom_OlderVersion_CreatesNewVersionWithRestoreSource()
    {
        var v1 = NoteVersion.First(NewNote().WithTitle("jedna"), Docs.Parse("a:A"), VersionSource.Desktop, T0);
        var v2 = v1.Next(Docs.Parse("a:A2"), NewNote().WithTitle("dva"), VersionSource.Web, T0.AddHours(1));

        var v3 = v2.RestoreFrom(v1, T0.AddHours(2));

        Assert.That(v3.Number, Is.EqualTo(3));
        Assert.That(v3.Source, Is.EqualTo(VersionSource.Restore));
        Assert.That(v3.BaseNumber, Is.EqualTo(1));
        Assert.That(Docs.Format(v3.Document), Is.EqualTo("a:A"));
        Assert.That(v3.Metadata.Title, Is.EqualTo("jedna"));
    }

    [Test]
    public void RestoreFrom_NewerOrSameVersion_Throws()
    {
        var v1 = NoteVersion.First(NewNote(), ContentDocument.Empty, VersionSource.Desktop, T0);

        Assert.Throws<ArgumentException>(() => v1.RestoreFrom(v1, T0));
    }
}
