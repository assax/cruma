using System.Text.Json.Nodes;
using Cruma.Content;
using Cruma.Desktop.Storage;
using Cruma.Notes;
using Cruma.Server.Tests;
using TestApi = Cruma.Server.Tests.Api;

namespace Cruma.Sync.Client.Tests;

/// <summary>
/// Integrační testy synchronizace desktopu proti testovacímu serveru (T-54; FR-27, FR-28, FR-37, SYN-003, SYN-004).
/// </summary>
public class SyncEngineTests
{
    private CrumaServerFactory server = null!;

    [OneTimeSetUp]
    public void StartServer() => server = new CrumaServerFactory().WithSetting("Cruma:Sync:MinimumClientVersion", "1.0.0");

    [OneTimeTearDown]
    public void StopServer() => server.Dispose();

    [Test]
    public async Task FirstSync_DownloadsDefaultCategoryAndWebNotes()
    {
        var web = await server.CreateUserAsync();
        var fromWeb = (await web.CreateNoteAsync("a:z telefonu")).Note;
        await using var desktop = await DesktopHarness.CreateAsync(server, web.UserId);

        var status = await desktop.Engine.SynchronizeAsync();

        Assert.That(status.State, Is.EqualTo(SyncState.Synced));
        Assert.That((await desktop.Store.ListCategoriesAsync()).Single().IsDefault, Is.True);
        Assert.That(Text((await desktop.Store.GetNoteAsync(fromWeb.Id)).Value.Document), Is.EqualTo("z telefonu"), "FR-27 akc. 2");
    }

    [Test]
    public async Task OfflineChanges_AfterReconnect_ArePushedWithoutUserAction()
    {
        var web = await server.CreateUserAsync();
        await using var desktop = await DesktopHarness.CreateAsync(server, web.UserId);
        await desktop.Engine.SynchronizeAsync();

        desktop.Transport.Offline = true;
        var note = (await desktop.Store.CreateNoteAsync(TestApi.NewId(), Doc("a:offline"))).Value;
        note = (await desktop.Store.SaveNoteAsync(note.Id, Changes(note, Doc("a:offline upraveno")))).Value;
        var offlineStatus = await desktop.Engine.SynchronizeAsync();

        desktop.Transport.Offline = false;
        var onlineStatus = await desktop.Engine.SynchronizeAsync();

        Assert.That(offlineStatus.State, Is.EqualTo(SyncState.Pending));
        Assert.That(offlineStatus.Offline, Is.True);
        Assert.That(onlineStatus.State, Is.EqualTo(SyncState.Synced));
        var onServer = await web.GetNoteAsync(note.Id);
        Assert.That(TestApi.Text(onServer), Is.EqualTo("a:offline upraveno"));
        Assert.That(onServer.Version, Is.EqualTo(1), "offline práce = jedna verze (FR-6 akc. 4)");
        Assert.That((await desktop.Store.GetNoteAsync(note.Id)).Value.ServerVersion, Is.EqualTo(1));
    }

    [Test]
    public async Task InterruptedPush_ResponseLost_NeitherLosesNorDuplicates()
    {
        var web = await server.CreateUserAsync();
        await using var desktop = await DesktopHarness.CreateAsync(server, web.UserId);
        await desktop.Engine.SynchronizeAsync();
        var note = (await desktop.Store.CreateNoteAsync(TestApi.NewId(), Doc("a:A"))).Value;
        await desktop.Engine.SynchronizeAsync();
        await desktop.Store.SaveNoteAsync(note.Id, Changes((await desktop.Store.GetNoteAsync(note.Id)).Value, Doc("a:A1")));

        desktop.Transport.LoseNextPushResponse = true;
        var interrupted = await desktop.Engine.SynchronizeAsync();
        await desktop.Store.SaveNoteAsync(note.Id, Changes((await desktop.Store.GetNoteAsync(note.Id)).Value, Doc("a:A12")));
        var retried = await desktop.Engine.SynchronizeAsync();

        Assert.That(interrupted.State, Is.EqualTo(SyncState.Pending));
        Assert.That(retried.State, Is.EqualTo(SyncState.Synced));
        var onServer = await web.GetNoteAsync(note.Id);
        Assert.That(TestApi.Text(onServer), Is.EqualTo("a:A12"));
        Assert.That(onServer.HasConflict, Is.False, "opakovaná změna se nesloučila sama se sebou");
        Assert.That((await web.GetAsync<Cruma.Api.Contracts.PagedResponse<Cruma.Api.Contracts.NoteDto>>("/api/v1/notes")).TotalCount, Is.EqualTo(1), "nezdvojilo se");
        Assert.That((await desktop.Store.GetSyncSummaryAsync()).Pending, Is.Zero);
        Assert.That(Text((await desktop.Store.GetNoteAsync(note.Id)).Value.Document), Is.EqualTo("A12"));
    }

    [Test]
    public async Task Acceptance1_DifferentParagraphsOnDesktopAndWeb_AreMerged()
    {
        var web = await server.CreateUserAsync();
        var created = (await web.CreateNoteAsync("a:A", "b:B")).Note;
        await using var desktop = await DesktopHarness.CreateAsync(server, web.UserId);
        await desktop.Engine.SynchronizeAsync();

        var local = (await desktop.Store.GetNoteAsync(created.Id)).Value;
        await desktop.Store.SaveNoteAsync(created.Id, Changes(local, Doc("a:A", "b:B desktop")));
        await web.UpdateNoteAsync(created, 1, "a:A web", "b:B");
        var status = await desktop.Engine.SynchronizeAsync();

        Assert.That(status.State, Is.EqualTo(SyncState.Synced));
        Assert.That(TestApi.Text(await web.GetNoteAsync(created.Id)), Is.EqualTo("a:A web b:B desktop"));
        Assert.That(Text((await desktop.Store.GetNoteAsync(created.Id)).Value.Document), Is.EqualTo("A web\nB desktop"));
    }

    [Test]
    public async Task Acceptance2_SameParagraphOnDesktopAndWeb_IsConflictOnBothSides()
    {
        var web = await server.CreateUserAsync();
        var created = (await web.CreateNoteAsync("a:A")).Note;
        await using var desktop = await DesktopHarness.CreateAsync(server, web.UserId);
        await desktop.Engine.SynchronizeAsync();

        await desktop.Store.SaveNoteAsync(created.Id, Changes((await desktop.Store.GetNoteAsync(created.Id)).Value, Doc("a:desktop")));
        await web.UpdateNoteAsync(created, 1, "a:web");
        var status = await desktop.Engine.SynchronizeAsync();

        Assert.That(status.State, Is.EqualTo(SyncState.Conflict));
        Assert.That((await web.GetNoteAsync(created.Id)).HasConflict, Is.True);
        var local = (await desktop.Store.GetNoteAsync(created.Id)).Value;
        Assert.That(local.HasConflict, Is.True);

        // Vyřešení na desktopu se odešle a konflikt zmizí všude (FR-28 akc. 3).
        await desktop.Store.ResolveConflictAsync(created.Id, "a", ConflictSide.Incoming, null);
        var resolvedStatus = await desktop.Engine.SynchronizeAsync();
        Assert.That(resolvedStatus.State, Is.EqualTo(SyncState.Synced));
        var resolved = await web.GetNoteAsync(created.Id);
        Assert.That(resolved.HasConflict, Is.False);
        Assert.That(TestApi.Text(resolved), Is.EqualTo("a:desktop"));
    }

    [Test]
    public async Task Acceptance4_EditOnDesktopWhileWebTrashes_KeepsEdit()
    {
        var web = await server.CreateUserAsync();
        var created = (await web.CreateNoteAsync("a:A")).Note;
        await using var desktop = await DesktopHarness.CreateAsync(server, web.UserId);
        await desktop.Engine.SynchronizeAsync();

        await desktop.Store.SaveNoteAsync(created.Id, Changes((await desktop.Store.GetNoteAsync(created.Id)).Value, Doc("a:A důležitá úprava")));
        await web.PostActionAsync(created.Id, "trash");
        await desktop.Engine.SynchronizeAsync();

        var onServer = await web.GetNoteAsync(created.Id);
        Assert.That(TestApi.Text(onServer), Is.EqualTo("a:A důležitá úprava"));
        Assert.That(onServer.State, Is.EqualTo(NoteState.Active));
        Assert.That((await desktop.Store.GetNoteAsync(created.Id)).Value.Metadata.State, Is.EqualTo(NoteState.Active));
    }

    [Test]
    public async Task Acceptance4_DeleteOnWebWhileDesktopEdits_EditRestoresNote()
    {
        var web = await server.CreateUserAsync();
        var created = (await web.CreateNoteAsync("a:A")).Note;
        await using var desktop = await DesktopHarness.CreateAsync(server, web.UserId);
        await desktop.Engine.SynchronizeAsync();
        await desktop.Store.SaveNoteAsync(created.Id, Changes((await desktop.Store.GetNoteAsync(created.Id)).Value, Doc("a:A upraveno offline")));

        await web.PostActionAsync(created.Id, "trash");
        await web.Http.DeleteAsync($"/api/v1/notes/{created.Id}");
        var status = await desktop.Engine.SynchronizeAsync();

        // Úprava trvale smazané poznámky se nesmí tiše ztratit: zůstane na desktopu jako zamítnutá změna (NFR-4, ERR-002).
        Assert.That(status.State, Is.EqualTo(SyncState.Error));
        Assert.That(Text((await desktop.Store.GetNoteAsync(created.Id)).Value.Document), Is.EqualTo("A upraveno offline"));
    }

    [Test]
    public async Task BelowMinimumVersion_StopsSyncAndKeepsChanges()
    {
        var web = await server.CreateUserAsync();
        await using var current = await DesktopHarness.CreateAsync(server, web.UserId);
        await current.Engine.SynchronizeAsync();
        await using var outdated = await DesktopHarness.CreateAsync(server, web.UserId, appVersion: "0.9.0");
        await outdated.Store.ApplyPulledAsync(new ChangeFeedEntry(1, SyncEntityType.Category,
            (await current.Store.ListCategoriesAsync()).Single().Id, null, false, Category: new CategoryPayload("Poznámky", true)));
        var note = (await outdated.Store.CreateNoteAsync(TestApi.NewId(), Doc("a:nesmí se ztratit"))).Value;

        var status = await outdated.Engine.SynchronizeAsync();

        Assert.That(status.State, Is.EqualTo(SyncState.Error));
        Assert.That(status.UpdateRequired, Is.True, "FR-37 akc. 1");
        Assert.That(outdated.Transport.PushCalls, Is.Zero);
        Assert.That((await outdated.Store.GetSyncSummaryAsync()).Pending, Is.EqualTo(1), "FR-37 akc. 2");
        Assert.That((await outdated.Store.GetNoteAsync(note.Id)).IsSuccess, Is.True);
    }

    [Test]
    public async Task CategoriesAndTags_CreatedOffline_SyncToServer()
    {
        var web = await server.CreateUserAsync();
        await using var desktop = await DesktopHarness.CreateAsync(server, web.UserId);
        await desktop.Engine.SynchronizeAsync();
        var category = (await desktop.Store.CreateCategoryAsync(TestApi.NewId(), "Z desktopu")).Value;
        var tag = (await desktop.Store.CreateTagAsync(TestApi.NewId(), "desktop")).Value;
        var note = (await desktop.Store.CreateNoteAsync(TestApi.NewId(), Doc("a:A"))).Value;
        await desktop.Store.SaveNoteAsync(note.Id, Changes(note) with { CategoryId = category.Id, TagIds = [tag.Id] });

        var status = await desktop.Engine.SynchronizeAsync();

        Assert.That(status.State, Is.EqualTo(SyncState.Synced));
        var onServer = await web.GetNoteAsync(note.Id);
        Assert.That(onServer.CategoryId, Is.EqualTo(category.Id));
        Assert.That(onServer.TagIds, Is.EqualTo(new[] { tag.Id }));
    }

    private static ContentDocument Doc(params string[] paragraphs) =>
        ContentDocument.Create(paragraphs.Select(notation =>
        {
            var separator = notation.IndexOf(':');
            return ContentBlock.FromJson(new JsonObject
            {
                ["type"] = "paragraph",
                ["attrs"] = new JsonObject { ["id"] = notation[..separator] },
                ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = notation[(separator + 1)..] }),
            });
        }));

    private static string Text(ContentDocument document) => PlainTextExtractor.Extract(null, document);

    private static LocalNoteChanges Changes(LocalNote note, ContentDocument? document = null) =>
        new(note.Metadata.Title, note.Metadata.CategoryId, [.. note.Metadata.TagIds], note.Metadata.Color, note.Metadata.IsPinned, document ?? note.Document);
}
