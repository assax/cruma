using Cruma.Desktop.Storage.Persistence;
using Cruma.Notes;
using Cruma.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cruma.Desktop.Storage.Tests;

/// <summary>Lokální úložiště desktopu (T-47, T-49; FR-26 akc. 1, FR-6 akc. 2, 4, SYN-004, PER-005).</summary>
public class LocalNotesStoreTests : LocalStoreTestBase
{
    [Test]
    public async Task Create_OfflineWithContentOnly_IsInDefaultCategoryWithPendingChange()
    {
        var created = await Store.CreateNoteAsync(Id(1), Doc("a:offline"));

        Assert.That(created.Value.Metadata.CategoryId, Is.EqualTo(DefaultCategoryId));
        Assert.That(created.Value.ServerVersion, Is.Null);
        var outgoing = await Store.TakeOutgoingChangesAsync();
        Assert.That(outgoing.Single().Change.Operation, Is.EqualTo(SyncOperation.Upsert));
        Assert.That(outgoing.Single().Change.BaseVersion, Is.Null);
        Assert.That(outgoing.Single().Change.Note!.DocumentJson, Does.Contain("offline"));
    }

    [Test]
    public async Task Create_BeforeFirstSync_WithoutDefaultCategory_Fails()
    {
        await using var db = Database.CreateContext();
        await db.Categories.ExecuteDeleteAsync();

        var created = await Store.CreateNoteAsync(Id(2), Doc("a:x"));

        Assert.That(created.ErrorCode, Is.EqualTo(LocalErrorCodes.DefaultCategoryMissing));
    }

    [Test]
    public async Task OfflineWork_ManyEdits_AreOnePendingChangeWithLatestContent()
    {
        var note = (await Store.CreateNoteAsync(Id(3), Doc("a:A"))).Value;
        for (var index = 1; index <= 5; index++)
        {
            note = (await Store.SaveNoteAsync(note.Id, Changes(note, Doc($"a:A{index}")))).Value;
        }

        await Store.ChangeStateAsync(note.Id, LocalStateChange.Archive);

        var outgoing = await Store.TakeOutgoingChangesAsync();
        Assert.That(outgoing, Has.Count.EqualTo(1), "offline práce se na serveru projeví jako jedna změna (FR-6 akc. 4)");
        Assert.That(outgoing[0].Change.Note!.DocumentJson, Does.Contain("A5"));
        Assert.That(outgoing[0].Change.Note!.State, Is.EqualTo(NoteState.Archived));
    }

    [Test]
    public async Task EditAfterSend_CreatesNewPendingChangeAndKeepsSentOne()
    {
        var note = (await Store.CreateNoteAsync(Id(4), Doc("a:A"))).Value;
        var first = await Store.TakeOutgoingChangesAsync();

        await Store.SaveNoteAsync(note.Id, Changes(note, Doc("a:A2")));
        var second = await Store.TakeOutgoingChangesAsync();

        Assert.That(second, Has.Count.EqualTo(2));
        Assert.That(second[0].Change.ChangeId, Is.EqualTo(first[0].Change.ChangeId), "odeslaná změna se opakuje se stejným identifikátorem (SYN-003)");
        Assert.That(second[1].Change.ChangeId, Is.Not.EqualTo(first[0].Change.ChangeId));
    }

    [Test]
    public async Task LocalVersions_EditSession_UpdatesWithinIntervalAndStartsNewAfter()
    {
        var note = (await Store.CreateNoteAsync(Id(5), Doc("a:A"))).Value;
        Clock.Advance(TimeSpan.FromMinutes(3));
        note = (await Store.SaveNoteAsync(note.Id, Changes(note, Doc("a:A1")))).Value;
        Clock.Advance(TimeSpan.FromMinutes(4));
        note = (await Store.SaveNoteAsync(note.Id, Changes(note, Doc("a:A2")))).Value;
        Clock.Advance(TimeSpan.FromMinutes(6));
        await Store.SaveNoteAsync(note.Id, Changes(note, Doc("a:A3")));

        Assert.That(await CountAsync(db => db.LocalVersions.CountAsync(version => version.NoteId == note.Id)), Is.EqualTo(2));
    }

    [Test]
    public async Task Acknowledge_LastPendingChange_DeletesLocalVersionsAndStoresServerVersion()
    {
        var note = (await Store.CreateNoteAsync(Id(6), Doc("a:A"))).Value;
        var outgoing = (await Store.TakeOutgoingChangesAsync()).Single();

        await Store.ApplyResultAsync(outgoing, new ChangeResult(outgoing.Change.ChangeId, ChangeOutcome.Applied, 1));

        Assert.That((await Store.GetNoteAsync(note.Id)).Value.ServerVersion, Is.EqualTo(1));
        Assert.That(await CountAsync(db => db.PendingChanges.CountAsync()), Is.Zero);
        Assert.That(await CountAsync(db => db.LocalVersions.CountAsync()), Is.Zero, "SYN-004");
    }

    [Test]
    public async Task Rejected_ChangeStaysPendingWithError()
    {
        await Store.CreateNoteAsync(Id(7), Doc("a:A"));
        var outgoing = (await Store.TakeOutgoingChangesAsync()).Single();

        await Store.ApplyResultAsync(outgoing, new ChangeResult(outgoing.Change.ChangeId, ChangeOutcome.Rejected, null, "category_not_found"));

        var summary = await Store.GetSyncSummaryAsync();
        Assert.That(summary.Pending, Is.EqualTo(1));
        Assert.That(summary.Rejected, Is.EqualTo(1));
        Assert.That(await CountAsync(db => db.LocalVersions.CountAsync()), Is.EqualTo(1), "neztrácí lokální práci (ERR-002)");
    }

    [Test]
    public async Task Pull_NoteWithPendingLocalChange_IsNotOverwritten()
    {
        var note = (await Store.CreateNoteAsync(Id(8), Doc("a:lokální"))).Value;

        await Store.ApplyPulledAsync(new ChangeFeedEntry(10, SyncEntityType.Note, note.Id, 5, false,
            Note: new NotePayload("ze serveru", DefaultCategoryId, [], null, false, NoteState.Active, Doc("a:serverová").ToJsonString())));

        var local = (await Store.GetNoteAsync(note.Id)).Value;
        Assert.That(Cruma.Content.PlainTextExtractor.Extract(null, local.Document), Is.EqualTo("lokální"));
        Assert.That(local.ServerVersion, Is.Null);
    }

    [Test]
    public async Task Pull_NewNoteAndDeletion_UpdateLocalStoreAndIndex()
    {
        await Store.ApplyPulledAsync(new ChangeFeedEntry(11, SyncEntityType.Note, Id(9), 3, false,
            Note: new NotePayload("Z webu", DefaultCategoryId, [], "yellow", true, NoteState.Active, Doc("a:Certifikát serveru").ToJsonString())));

        var found = await Store.SearchNotesAsync("certif", null, 0, 10);
        var pulled = (await Store.GetNoteAsync(Id(9))).Value;
        Assert.That(found.Items.Single().Id, Is.EqualTo(Id(9)));
        Assert.That(pulled.ServerVersion, Is.EqualTo(3));
        Assert.That(pulled.Metadata.IsPinned, Is.True);

        await Store.ApplyPulledAsync(new ChangeFeedEntry(12, SyncEntityType.Note, Id(9), null, true));

        Assert.That((await Store.GetNoteAsync(Id(9))).ErrorCode, Is.EqualTo(LocalErrorCodes.NotFound));
        Assert.That((await Store.SearchNotesAsync("certif", null, 0, 10)).TotalCount, Is.Zero);
    }

    [Test]
    public async Task DeletePermanently_OnlyFromTrash_QueuesDelete()
    {
        var note = (await Store.CreateNoteAsync(Id(10), Doc("a:A"))).Value;
        Assert.That((await Store.DeleteNotePermanentlyAsync(note.Id)).ErrorCode, Is.EqualTo(NotesErrorCodes.NoteNotInTrash));

        await Store.ChangeStateAsync(note.Id, LocalStateChange.Trash);
        var deleted = await Store.DeleteNotePermanentlyAsync(note.Id);

        Assert.That(deleted.IsSuccess, Is.True);
        var outgoing = await Store.TakeOutgoingChangesAsync();
        Assert.That(outgoing.Single().Change.Operation, Is.EqualTo(SyncOperation.Delete));
    }

    [Test]
    public async Task OfflineOperations_ListSearchArchiveTrash_Work()
    {
        var pinned = (await Store.CreateNoteAsync(Id(11), Doc("a:připnutá"))).Value;
        await Store.SaveNoteAsync(pinned.Id, Changes(pinned) with { IsPinned = true });
        Clock.Advance(TimeSpan.FromSeconds(1));
        await Store.CreateNoteAsync(Id(12), Doc("a:novější"));
        var archived = (await Store.CreateNoteAsync(Id(13), Doc("a:archivní text"))).Value;
        await Store.ChangeStateAsync(archived.Id, LocalStateChange.Archive);

        var active = await Store.ListNotesAsync(NoteState.Active, null, null, 0, 10);
        var archive = await Store.ListNotesAsync(NoteState.Archived, null, null, 0, 10);
        var search = await Store.SearchNotesAsync("archivni", NoteState.Archived, 0, 10);

        Assert.That(active.Items.Select(note => note.Id), Is.EqualTo(new[] { pinned.Id, Id(12) }));
        Assert.That(archive.Items.Single().Id, Is.EqualTo(archived.Id));
        Assert.That(search.Items.Single().Id, Is.EqualTo(archived.Id));
    }

    [Test]
    public async Task Categories_DeleteMovesNotesToDefault_DefaultCannotBeDeleted()
    {
        var work = (await Store.CreateCategoryAsync(Id(20), "Práce")).Value;
        var note = (await Store.CreateNoteAsync(Id(21), Doc("a:A"))).Value;
        await Store.SaveNoteAsync(note.Id, Changes(note, categoryId: work.Id));

        Assert.That((await Store.DeleteCategoryAsync(DefaultCategoryId)).ErrorCode, Is.EqualTo(NotesErrorCodes.DefaultCategoryCannotBeDeleted));
        Assert.That((await Store.DeleteCategoryAsync(work.Id)).IsSuccess, Is.True);
        Assert.That((await Store.GetNoteAsync(note.Id)).Value.Metadata.CategoryId, Is.EqualTo(DefaultCategoryId));
    }

    [Test]
    public async Task Tags_DeleteRemovesTagFromNotes()
    {
        var tag = (await Store.CreateTagAsync(Id(22), "důležité")).Value;
        var note = (await Store.CreateNoteAsync(Id(23), Doc("a:A"))).Value;
        await Store.SaveNoteAsync(note.Id, Changes(note, tagIds: [tag.Id]));

        await Store.DeleteTagAsync(tag.Id);

        Assert.That((await Store.GetNoteAsync(note.Id)).Value.Metadata.TagIds, Is.Empty);
    }

    [Test]
    public async Task ResolveConflict_Locally_RemovesConflictAndQueuesChange()
    {
        var origin = new Cruma.Content.ConflictOrigin("web", DateTimeOffset.UnixEpoch);
        var conflicted = new Cruma.Content.ConflictBlock("a", Doc("a:web").Blocks[0], origin, Doc("a:desktop").Blocks[0], origin);
        await Store.ApplyPulledAsync(new ChangeFeedEntry(13, SyncEntityType.Note, Id(24), 4, false,
            Note: new NotePayload(null, DefaultCategoryId, [], null, false, NoteState.Active, Cruma.Content.ContentDocument.Create([conflicted.ToBlock()]).ToJsonString())));
        Assert.That((await Store.GetNoteAsync(Id(24))).Value.HasConflict, Is.True);

        var resolved = await Store.ResolveConflictAsync(Id(24), "a", Cruma.Content.ConflictSide.Incoming, null);

        Assert.That(resolved.Value.HasConflict, Is.False);
        Assert.That((await Store.TakeOutgoingChangesAsync()).Single().Change.BaseVersion, Is.EqualTo(4));
    }

    [Test]
    public async Task Open_WithPendingMigrations_BacksUpExistingFile()
    {
        var folder = Path.Combine(Folder, "backup");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, LocalDatabase.FileName(UserId));
        await using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE legacy (id INTEGER)";
            await command.ExecuteNonQueryAsync();
        }

        await LocalDatabase.OpenAsync(folder, UserId, Clock, NullLogger.Instance);

        Assert.That(Directory.GetFiles(folder, "*.bak"), Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Schema_ContainsNoTokensOrSecrets()
    {
        await using var db = Database.CreateContext();
        var columns = db.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()).Select(property => property.Name.ToLowerInvariant()).ToList();

        Assert.That(columns, Has.None.Contains("token").And.None.Contains("secret").And.None.Contains("password"), "PER-005");
    }

    private async Task<int> CountAsync(Func<CrumaLocalDbContext, Task<int>> query)
    {
        await using var db = Database.CreateContext();
        return await query(db);
    }
}
