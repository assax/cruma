using Bunit;
using Cruma.Content;
using Cruma.Notes;
using Cruma.Ui.Components;
using Cruma.Ui.Components.Layout;
using Cruma.Ui.Components.Notes;
using Cruma.Ui.Editor;
using Cruma.Ui.Pages;
using Cruma.Ui.Services;
using Moq;

namespace Cruma.Ui.Tests;

/// <summary>Rychlé zachycení (T-39; FR-1, UI-005, FR-30).</summary>
public class QuickCaptureTests : UiTestContext
{
    [Test]
    public async Task Save_TypedContentOnly_CreatesNoteWithoutAnyOtherInput()
    {
        ContentDocument? saved = null;
        NoteData.Setup(notes => notes.CreateAsync(It.IsAny<Guid>(), It.IsAny<ContentDocument>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ContentDocument, CancellationToken>((_, document, _) => saved = document)
            .ReturnsAsync(DataResult<NoteCreateResult>.Success(new NoteCreateResult(Note(Guid.Empty, "x"), Queued: false)));
        var cut = Render<QuickCapture>();

        await TypeAsync(cut, "Rychlá myšlenka");
        cut.Find("button.primary").Click();

        Assert.That(saved, Is.Not.Null);
        Assert.That(PlainTextExtractor.Extract(null, saved!), Is.EqualTo("Rychlá myšlenka"));
        Assert.That(cut.Markup, Does.Contain("Uloženo."));
        Assert.That(cut.FindAll("input, select"), Is.Empty, "žádné povinné pole navíc (FR-1 akc. 2)");
    }

    [Test]
    public void Save_EmptyContent_DoesNotCreate()
    {
        var cut = Render<QuickCapture>();

        cut.Find("button.primary").Click();

        NoteData.Verify(notes => notes.CreateAsync(It.IsAny<Guid>(), It.IsAny<ContentDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Save_WhileOffline_ShowsQueuedMessage()
    {
        Connectivity.SetupGet(connectivity => connectivity.IsOnline).Returns(false);
        Capabilities.SetupGet(capabilities => capabilities.QueuesNewNotesOffline).Returns(true);
        NoteData.Setup(notes => notes.CreateAsync(It.IsAny<Guid>(), It.IsAny<ContentDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<NoteCreateResult>.Success(new NoteCreateResult(null, Queued: true)));
        var cut = Render<QuickCapture>();

        await TypeAsync(cut, "bez sítě");
        cut.Find("button.primary").Click();

        Assert.That(cut.Markup, Does.Contain("fronty"));
    }

    private static async Task TypeAsync(IRenderedComponent<QuickCapture> cut, string text)
    {
        var editor = cut.FindComponent<CrumaEditor>();
        await cut.InvokeAsync(() => editor.Instance.OnContentChanged(
            $$"""[{"type":"paragraph","attrs":{"id":"p1"},"content":[{"type":"text","text":"{{text}}"}]}]"""));
    }
}

/// <summary>Přehled, archiv a koš (T-38; FR-5).</summary>
public class NoteListViewTests : UiTestContext
{
    [Test]
    public void Trash_DeletePermanently_RequiresExplicitConfirmation()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        NoteData.Setup(notes => notes.ListAsync(NoteState.Trashed, null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<PageResult<NoteItem>>.Success(new PageResult<NoteItem>([Note(id, "v koši", NoteState.Trashed)], 1)));
        NoteData.Setup(notes => notes.DeletePermanentlyAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(DataResult.Success());
        var cut = Render<NoteListView>(parameters => parameters.Add(view => view.State, NoteState.Trashed));

        cut.FindAll("button").Single(button => button.TextContent == "Smazat natrvalo").Click();
        NoteData.Verify(notes => notes.DeletePermanentlyAsync(id, It.IsAny<CancellationToken>()), Times.Never);

        cut.FindAll("button").Single(button => button.TextContent == "Smazat").Click();
        NoteData.Verify(notes => notes.DeletePermanentlyAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Trash_Restore_ChangesStateToActive()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        NoteData.Setup(notes => notes.ListAsync(NoteState.Trashed, null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<PageResult<NoteItem>>.Success(new PageResult<NoteItem>([Note(id, "v koši", NoteState.Trashed)], 1)));
        NoteData.Setup(notes => notes.ChangeStateAsync(id, NoteStateChange.Restore, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<NoteSaveResult>.Success(new NoteSaveResult(Note(id, "v koši"), "applied", [], [])));
        var cut = Render<NoteListView>(parameters => parameters.Add(view => view.State, NoteState.Trashed));

        cut.FindAll("button").Single(button => button.TextContent == "Obnovit").Click();

        NoteData.Verify(notes => notes.ChangeStateAsync(id, NoteStateChange.Restore, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void Offline_ShellWorkingOffline_KeepsActionsEnabled()
    {
        Connectivity.SetupGet(connectivity => connectivity.IsOnline).Returns(false);
        Capabilities.SetupGet(capabilities => capabilities.WorksOffline).Returns(true);
        NoteData.Setup(notes => notes.ListAsync(NoteState.Archived, null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<PageResult<NoteItem>>.Success(new PageResult<NoteItem>([Note(Guid.Parse("00000000-0000-0000-0000-000000000004"), "archiv", NoteState.Archived)], 1)));

        var cut = Render<NoteListView>(parameters => parameters.Add(view => view.State, NoteState.Archived));

        Assert.That(cut.FindAll("button").Single(button => button.TextContent == "Vrátit z archivu").HasAttribute("disabled"), Is.False, "FR-26 akc. 1");
    }

    [Test]
    public void Offline_ActionsOnExistingNotes_AreDisabled()
    {
        Connectivity.SetupGet(connectivity => connectivity.IsOnline).Returns(false);
        NoteData.Setup(notes => notes.ListAsync(NoteState.Archived, null, null, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<PageResult<NoteItem>>.Success(new PageResult<NoteItem>([Note(Guid.Parse("00000000-0000-0000-0000-000000000003"), "archiv", NoteState.Archived)], 1)));

        var cut = Render<NoteListView>(parameters => parameters.Add(view => view.State, NoteState.Archived));

        Assert.That(cut.FindAll("button").Single(button => button.TextContent == "Vrátit z archivu").HasAttribute("disabled"), Is.True);
    }
}

/// <summary>Řešení konfliktů (T-43; FR-28 akc. 2, 3).</summary>
public class ConflictPanelTests : UiTestContext
{
    [Test]
    public void Render_ConflictBlock_ShowsBothVariantsAndResolvesChosenOne()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var origin = new ConflictOrigin("web", DateTimeOffset.UnixEpoch);
        var conflict = new ConflictBlock("a", Paragraph("a", "verze z webu"), origin, Paragraph("a", "verze z desktopu"), origin with { ClientType = "desktop" });
        var note = Note(id, "x", version: 3, document: ContentDocument.Create([conflict.ToBlock()])) with { HasConflict = true };
        NoteData.Setup(notes => notes.ResolveConflictAsync(id, 3, "a", ConflictSide.Incoming, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<NoteSaveResult>.Success(new NoteSaveResult(Note(id, "verze z desktopu", version: 4), "applied", [], [])));
        NoteSaveResult? resolved = null;

        var cut = Render<ConflictPanel>(parameters => parameters
            .Add(panel => panel.Note, note)
            .Add(panel => panel.Resolved, result => resolved = result));

        Assert.That(cut.Markup, Does.Contain("verze z webu").And.Contain("verze z desktopu"));
        cut.FindAll("button").Where(button => button.TextContent == "Ponechat tuto").ElementAt(1).Click();
        Assert.That(resolved!.Note.Version, Is.EqualTo(4));
    }

    [Test]
    public void ManualEdit_ResolvesWithEditedParagraphKeepingBlockId()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var origin = new ConflictOrigin("web", DateTimeOffset.UnixEpoch);
        var conflict = new ConflictBlock("a", Paragraph("a", "A1"), origin, Paragraph("a", "A2"), origin);
        var note = Note(id, "x", version: 2, document: ContentDocument.Create([conflict.ToBlock()])) with { HasConflict = true };
        ContentBlock? edited = null;
        NoteData.Setup(notes => notes.ResolveConflictAsync(id, 2, "a", null, It.IsAny<ContentBlock>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, long, string, ConflictSide?, ContentBlock?, CancellationToken>((_, _, _, _, block, _) => edited = block)
            .ReturnsAsync(DataResult<NoteSaveResult>.Success(new NoteSaveResult(Note(id, "spojeno", version: 3), "applied", [], [])));
        var cut = Render<ConflictPanel>(parameters => parameters.Add(panel => panel.Note, note));

        cut.FindAll("button").Single(button => button.TextContent == "Upravit ručně").Click();
        cut.Find("textarea").Change("spojeno");
        cut.FindAll("button").Single(button => button.TextContent == "Uložit úpravu").Click();

        Assert.That(edited!.Id, Is.EqualTo("a"));
        Assert.That(PlainTextExtractor.Extract(null, ContentDocument.Create([edited])), Is.EqualTo("spojeno"));
    }
}

/// <summary>Layout: motiv (T-33, FR-34), stav offline (FR-29 akc. 4), přihlášení.</summary>
public class MainLayoutTests : UiTestContext
{
    [Test]
    public void ThemeToggle_SwitchesToDarkAndStoresPreference()
    {
        SessionData.Setup(session => session.GetSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<SessionInfo?>.Success(new SessionInfo(Guid.Parse("00000000-0000-0000-0000-0000000000a1"))));
        var cut = Render<MainLayout>();

        Assert.That(cut.Find(".cruma-app").GetAttribute("data-theme"), Is.EqualTo("light"));
        cut.Find("button.theme-toggle").Click();

        Assert.That(cut.Find(".cruma-app").GetAttribute("data-theme"), Is.EqualTo("dark"));
        Preferences.Verify(preferences => preferences.SetAsync(CrumaUiState.ThemeKey, "dark"), Times.Once);
    }

    [Test]
    public void Offline_ShowsIndicator()
    {
        Connectivity.SetupGet(connectivity => connectivity.IsOnline).Returns(false);
        SessionData.Setup(session => session.GetSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<SessionInfo?>.Success(new SessionInfo(Guid.Parse("00000000-0000-0000-0000-0000000000a1"))));

        var cut = Render<MainLayout>();

        Assert.That(cut.Find(".offline").TextContent, Is.EqualTo("Offline"));
    }

    [Test]
    public void NoSession_ShowsSignIn()
    {
        SessionData.Setup(session => session.GetSessionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DataResult<SessionInfo?>.Success(null));
        SessionData.Setup(session => session.GetSignInOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<SignInOptions>.Success(new SignInOptions(["google"], false)));
        SessionData.Setup(session => session.ExternalSignInUrl("google", "/")).Returns("auth/sign-in/google?returnUrl=%2F");

        var cut = Render<MainLayout>();

        Assert.That(cut.Find("a.primary").GetAttribute("href"), Is.EqualTo("auth/sign-in/google?returnUrl=%2F"));
        Assert.That(cut.FindAll("nav"), Is.Empty);
    }
}

/// <summary>Detail poznámky: šířka editoru podle schopností (T-44, FR-8), úpravy bez připojení (FR-30 akc. 5).</summary>
public class NoteDetailTests : UiTestContext
{
    private static readonly Guid NoteId = Guid.Parse("00000000-0000-0000-0000-000000000020");

    [SetUp]
    public void SetUpNote() =>
        NoteData.Setup(notes => notes.GetAsync(NoteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DataResult<NoteItem>.Success(Note(NoteId, "obsah")));

    [Test]
    public void WidthModes_OnlyWithCapability()
    {
        Capabilities.SetupGet(capabilities => capabilities.EditorWidthModes).Returns(true);
        var withModes = Render<NoteDetail>(parameters => parameters.Add(page => page.Id, NoteId));

        Assert.That(withModes.FindAll("select").Any(select => select.InnerHtml.Contains("Rozšířená")), Is.True);
        Assert.That(withModes.Find(".note-detail").ClassList, Does.Contain("width-standard"));
    }

    [Test]
    public void WidthModes_WithoutCapability_AreResponsive()
    {
        var cut = Render<NoteDetail>(parameters => parameters.Add(page => page.Id, NoteId));

        Assert.That(cut.FindAll("select").Any(select => select.InnerHtml.Contains("Rozšířená")), Is.False);
        Assert.That(cut.Find(".note-detail").ClassList, Does.Contain("width-responsive"));
    }

    [Test]
    public void Offline_ThinClient_DisablesEditingOfExistingNote()
    {
        Connectivity.SetupGet(connectivity => connectivity.IsOnline).Returns(false);

        var cut = Render<NoteDetail>(parameters => parameters.Add(page => page.Id, NoteId));

        Assert.That(cut.Find("input.title").HasAttribute("disabled"), Is.True);
        Assert.That(cut.FindAll("button").Single(button => button.TextContent == "Archivovat").HasAttribute("disabled"), Is.True);
    }

    [Test]
    public void TitleChange_SavesAgainstLoadedVersion()
    {
        NoteData.Setup(notes => notes.SaveAsync(It.IsAny<NoteItem>(), It.IsAny<NoteChanges>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteItem basedOn, NoteChanges changes, CancellationToken _) =>
                DataResult<NoteSaveResult>.Success(new NoteSaveResult(basedOn with { Version = basedOn.Version + 1 }, "applied", [], [])));
        var cut = Render<NoteDetail>(parameters => parameters.Add(page => page.Id, NoteId));

        cut.Find("input.title").Change("Nový název");

        cut.WaitForAssertion(() => NoteData.Verify(notes => notes.SaveAsync(
            It.Is<NoteItem>(item => item.Version == 1),
            It.Is<NoteChanges>(changes => changes.Title == "Nový název" && changes.CategoryId == DefaultCategoryId),
            It.IsAny<CancellationToken>()), Times.Once));
    }
}
