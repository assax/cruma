using Cruma.Content;
using Cruma.Notes;
using static Cruma.Versioning.Tests.TestNotes;

namespace Cruma.Versioning.Tests;

/// <summary>Merge poznámky na serveru a vyřešení konfliktu (scénář 3, FR-28 akc. 1–4, VER-004).</summary>
public class NoteMergeTests
{
    private NoteVersion v1 = null!;

    [SetUp]
    public void CreateBase()
    {
        v1 = NoteVersion.First(NewNote().WithTitle("Poznámka"), Docs.Parse("a:A b:B"), VersionSource.Desktop, T0);
    }

    [Test]
    public void Merge_BaseIsLatestVersion_AppliesIncomingWithClientSource()
    {
        var change = Change(Docs.Parse("a:A1 b:B"), v1.Metadata, VersionSource.Web);

        var result = NoteMerge.Merge(v1, v1, change);
        var v2 = result.ToVersion(v1, change, v1.Number, T0.AddMinutes(1));

        Assert.That(result.Outcome, Is.EqualTo(NoteMergeOutcome.Applied));
        Assert.That(Docs.Format(v2.Document), Is.EqualTo("a:A1 b:B"));
        Assert.That(v2.Source, Is.EqualTo(VersionSource.Web));
        Assert.That(v2.Number, Is.EqualTo(2));
    }

    [Test]
    public void Merge_Acceptance1_DifferentParagraphs_MergedVersionWithBothChanges()
    {
        var v2 = v1.Next(Docs.Parse("a:A-web b:B"), v1.Metadata, VersionSource.Web, T0.AddMinutes(5));
        var change = Change(Docs.Parse("a:A b:B-desktop"), v1.Metadata, VersionSource.Desktop);

        var result = NoteMerge.Merge(v1, v2, change);
        var v3 = result.ToVersion(v2, change, v1.Number, T0.AddMinutes(6));

        Assert.That(result.Outcome, Is.EqualTo(NoteMergeOutcome.Merged));
        Assert.That(Docs.Format(v3.Document), Is.EqualTo("a:A-web b:B-desktop"));
        Assert.That(v3.Source, Is.EqualTo(VersionSource.Merge));
        Assert.That(v3.BaseNumber, Is.EqualTo(1));
        Assert.That(v3.HasConflict, Is.False);
    }

    [Test]
    public void Merge_Acceptance2And3_SameParagraph_ConflictThenResolvedInNewVersion()
    {
        var v2 = v1.Next(Docs.Parse("a:A-web b:B"), v1.Metadata, VersionSource.Web, T0.AddMinutes(5));
        var change = Change(Docs.Parse("a:A-desktop b:B"), v1.Metadata, VersionSource.Desktop);

        var result = NoteMerge.Merge(v1, v2, change);
        var v3 = result.ToVersion(v2, change, v1.Number, T0.AddMinutes(6));

        Assert.That(result.Outcome, Is.EqualTo(NoteMergeOutcome.Conflict));
        Assert.That(v3.HasConflict, Is.True);
        Assert.That(Docs.Format(v3.Document), Is.EqualTo("a:!A-web|A-desktop b:B"));

        var chosen = ConflictResolution.ChooseVariant(v3.Document, "a", ConflictSide.Incoming);
        var v4 = ConflictResolution.CreateResolvedVersion(v3, chosen, v3.Metadata, VersionSource.Web, T0.AddMinutes(7));

        Assert.That(v4.Number, Is.EqualTo(4));
        Assert.That(v4.HasConflict, Is.False);
        Assert.That(Docs.Format(v4.Document), Is.EqualTo("a:A-desktop b:B"));
        Assert.That(v3.HasConflict, Is.True, "předchozí verze se nepřepisuje");
    }

    [Test]
    public void ReplaceWithEdit_ManualResolution_KeepsBlockIdentifier()
    {
        var conflicted = Docs.Parse("a:!A1|A2 b:B");

        var resolved = ConflictResolution.ReplaceWithEdit(conflicted, "a", Docs.Block("a", "A1 i A2"));

        Assert.That(Docs.Format(resolved), Is.EqualTo("a:A1 i A2 b:B"));
    }

    [Test]
    public void ReplaceWithEdit_DifferentBlockId_Throws()
    {
        Assert.Throws<ArgumentException>(() => ConflictResolution.ReplaceWithEdit(Docs.Parse("a:!A1|A2"), "a", Docs.Block("x", "x")));
    }

    [Test]
    public void ChooseVariant_BlockIsNotConflict_Throws()
    {
        Assert.Throws<ArgumentException>(() => ConflictResolution.ChooseVariant(Docs.Parse("a:A"), "a", ConflictSide.Current));
    }

    [Test]
    public void Merge_Acceptance4_EditOfNoteTrashedOnServer_RestoresNoteWithEdit()
    {
        var v2 = v1.Next(v1.Document, v1.Metadata.MoveToTrash(), VersionSource.Web, T0.AddMinutes(5));
        var change = Change(Docs.Parse("a:A b:B-desktop"), v1.Metadata, VersionSource.Desktop);

        var result = NoteMerge.Merge(v1, v2, change);

        Assert.That(result.Metadata.State, Is.EqualTo(NoteState.Active));
        Assert.That(Docs.Format(result.Document), Is.EqualTo("a:A b:B-desktop"));
        Assert.That(result.Notices, Does.Contain(new MergeNotice(MergeNoticeKind.NoteRestoredFromTrash)));
    }

    [Test]
    public void Merge_Acceptance4_TrashOfNoteEditedOnServer_KeepsEditInTrashedNote()
    {
        var v2 = v1.Next(Docs.Parse("a:A-web b:B"), v1.Metadata, VersionSource.Web, T0.AddMinutes(5));
        var change = Change(v1.Document, v1.Metadata.MoveToTrash(), VersionSource.Desktop);

        var result = NoteMerge.Merge(v1, v2, change);

        Assert.That(result.Metadata.State, Is.EqualTo(NoteState.Trashed));
        Assert.That(Docs.Format(result.Document), Is.EqualTo("a:A-web b:B"));
        Assert.That(result.Notices, Does.Contain(new MergeNotice(MergeNoticeKind.NoteTrashedAfterConcurrentEdit)));
    }

    [Test]
    public void Merge_TrashedOnServerAndArchivedByClientWithEdit_LaterReceivedStateWins()
    {
        var v2 = v1.Next(v1.Document, v1.Metadata.MoveToTrash(), VersionSource.Web, T0.AddMinutes(5));
        var change = Change(Docs.Parse("a:A1 b:B"), v1.Metadata.Archive(), VersionSource.Desktop);

        var result = NoteMerge.Merge(v1, v2, change);

        Assert.That(result.Metadata.State, Is.EqualTo(NoteState.Archived));
        Assert.That(result.OverwrittenValues.Select(value => value.Field), Is.EqualTo(new[] { MetadataMerge.StateField }));
    }

    [Test]
    public void Merge_TitleChangedOnBothSides_OverwrittenValueIsInNewVersion()
    {
        var v2 = v1.Next(v1.Document, v1.Metadata.WithTitle("web"), VersionSource.Web, T0.AddMinutes(5));
        var change = Change(v1.Document, v1.Metadata.WithTitle("desktop"), VersionSource.Desktop);

        var result = NoteMerge.Merge(v1, v2, change);
        var v3 = result.ToVersion(v2, change, v1.Number, T0.AddMinutes(6));

        Assert.That(v3.Metadata.Title, Is.EqualTo("desktop"));
        Assert.That(v3.OverwrittenValues, Is.EqualTo(new[] { new OverwrittenValue(MetadataMerge.TitleField, "web", "desktop") }));
    }

    [Test]
    public void Merge_BaseNewerThanCurrent_Throws()
    {
        var v2 = v1.Next(v1.Document, v1.Metadata, VersionSource.Web, T0);

        Assert.Throws<ArgumentException>(() => NoteMerge.Merge(v2, v1, Change(ContentDocument.Empty, v1.Metadata, VersionSource.Web)));
    }

    private static IncomingNoteChange Change(ContentDocument document, Note metadata, VersionSource source) =>
        new(document, metadata, source, T0.AddMinutes(4));
}
