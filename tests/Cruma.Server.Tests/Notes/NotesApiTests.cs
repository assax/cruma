using System.Net;
using System.Net.Http.Json;
using Cruma.Api.Contracts;
using Cruma.Notes;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cruma.Server.Tests.Notes;

/// <summary>REST API poznámek a jednotná zápisová cesta (T-24, T-27; FR-1..FR-5, FR-28, FR-29).</summary>
public class NotesApiTests
{
    private CrumaServerFactory factory = null!;

    [OneTimeSetUp]
    public void StartServer() => factory = new CrumaServerFactory();

    [OneTimeTearDown]
    public void StopServer() => factory.Dispose();

    [Test]
    public async Task Create_OnlyContent_IsSavedAsVersionOneInDefaultCategory()
    {
        var user = await factory.CreateUserAsync();

        var created = await user.CreateNoteAsync("a:první poznámka");

        var defaultCategory = await user.DefaultCategoryAsync();
        Assert.That(created.Outcome, Is.EqualTo("applied"));
        Assert.That(created.Note.Version, Is.EqualTo(1));
        Assert.That(created.Note.CategoryId, Is.EqualTo(defaultCategory.Id));
        Assert.That(created.Note.State, Is.EqualTo(NoteState.Active));
        Assert.That(created.Note.Title, Is.Null);
        Assert.That(Api.Text(await user.GetNoteAsync(created.Note.Id)), Is.EqualTo("a:první poznámka"));
    }

    [Test]
    public async Task Create_SameIdTwice_DoesNotDuplicate()
    {
        var user = await factory.CreateUserAsync();
        var id = Api.NewId();

        await user.CreateNoteAsync(id, "a:jednou");
        var again = await user.CreateNoteAsync(id, "a:jednou");

        var list = await user.GetAsync<PagedResponse<NoteDto>>("/api/v1/notes");
        Assert.That(again.Note.Version, Is.EqualTo(1));
        Assert.That(list.TotalCount, Is.EqualTo(1));
        Assert.That(await VersionCountAsync(user.UserId, id), Is.EqualTo(1));
    }

    [Test]
    public async Task Update_FromLatestVersion_CreatesNextVersion()
    {
        var user = await factory.CreateUserAsync();
        var created = await user.CreateNoteAsync("a:A");

        var updated = await user.UpdateNoteAsync(created.Note, 1, "a:A1");

        Assert.That(updated.Outcome, Is.EqualTo("applied"));
        Assert.That(updated.Note.Version, Is.EqualTo(2));
        Assert.That(Api.Text(updated.Note), Is.EqualTo("a:A1"));
    }

    [Test]
    public async Task Update_Acceptance1_DifferentParagraphsFromSameBase_AreMerged()
    {
        var user = await factory.CreateUserAsync();
        var created = await user.CreateNoteAsync("a:A", "b:B");

        await user.UpdateNoteAsync(created.Note, 1, "a:A-web", "b:B");
        var second = await user.UpdateNoteAsync(created.Note, 1, "a:A", "b:B-desktop");

        Assert.That(second.Outcome, Is.EqualTo("merged"));
        Assert.That(second.Note.Version, Is.EqualTo(3));
        Assert.That(Api.Text(second.Note), Is.EqualTo("a:A-web b:B-desktop"));
    }

    [Test]
    public async Task Update_Acceptance2And3_SameParagraph_ConflictIsStoredAndResolvable()
    {
        var user = await factory.CreateUserAsync();
        var created = await user.CreateNoteAsync("a:A", "b:B");

        await user.UpdateNoteAsync(created.Note, 1, "a:A-web", "b:B");
        var conflicted = await user.UpdateNoteAsync(created.Note, 1, "a:A-desktop", "b:B");

        Assert.That(conflicted.Outcome, Is.EqualTo("conflict"));
        Assert.That(conflicted.Note.HasConflict, Is.True);
        Assert.That(Api.Text(conflicted.Note), Is.EqualTo("a:!A-web|A-desktop b:B"));

        var resolve = await user.Http.PostAsJsonAsync(
            $"/api/v1/notes/{created.Note.Id}/conflicts/a/resolve",
            new ResolveConflictRequest(conflicted.Note.Version, "incoming", null),
            Api.Json);
        var resolved = await Api.ReadAsync<NoteWriteResponse>(resolve, HttpStatusCode.OK);

        Assert.That(resolved.Note.HasConflict, Is.False);
        Assert.That(resolved.Note.Version, Is.EqualTo(conflicted.Note.Version + 1));
        Assert.That(Api.Text(resolved.Note), Is.EqualTo("a:A-desktop b:B"));
    }

    [Test]
    public async Task ResolveConflict_EditedBlock_ReplacesConflict()
    {
        var user = await factory.CreateUserAsync();
        var created = await user.CreateNoteAsync("a:A");
        await user.UpdateNoteAsync(created.Note, 1, "a:A1");
        var conflicted = await user.UpdateNoteAsync(created.Note, 1, "a:A2");

        var edited = System.Text.Json.JsonSerializer.SerializeToElement(Api.Paragraph("a:A1 a A2").ToJson());
        var resolve = await user.Http.PostAsJsonAsync(
            $"/api/v1/notes/{created.Note.Id}/conflicts/a/resolve",
            new ResolveConflictRequest(conflicted.Note.Version, null, edited),
            Api.Json);

        var resolved = await Api.ReadAsync<NoteWriteResponse>(resolve, HttpStatusCode.OK);
        Assert.That(Api.Text(resolved.Note), Is.EqualTo("a:A1 a A2"));
    }

    [Test]
    public async Task Update_Acceptance4_EditOfNoteTrashedMeanwhile_RestoresNote()
    {
        var user = await factory.CreateUserAsync();
        var created = await user.CreateNoteAsync("a:A");

        await user.PostActionAsync(created.Note.Id, "trash");
        var edited = await user.UpdateNoteAsync(created.Note, 1, "a:A upraveno");

        Assert.That(edited.Note.State, Is.EqualTo(NoteState.Active));
        Assert.That(Api.Text(edited.Note), Is.EqualTo("a:A upraveno"));
        Assert.That(edited.Notices.Select(notice => notice.Kind), Does.Contain("NoteRestoredFromTrash"));
    }

    [Test]
    public async Task ArchiveTrashRestore_StatesAndLists_FollowFr5()
    {
        var user = await factory.CreateUserAsync();
        var note = (await user.CreateNoteAsync("a:A")).Note;

        await user.PostActionAsync(note.Id, "archive");
        Assert.That((await user.GetAsync<PagedResponse<NoteDto>>("/api/v1/notes")).TotalCount, Is.Zero);
        Assert.That((await user.GetAsync<PagedResponse<NoteDto>>("/api/v1/notes?state=Archived")).TotalCount, Is.EqualTo(1));

        await user.PostActionAsync(note.Id, "trash");
        Assert.That((await user.GetAsync<PagedResponse<NoteDto>>("/api/v1/notes?state=Archived")).TotalCount, Is.Zero);
        Assert.That((await user.GetAsync<PagedResponse<NoteDto>>("/api/v1/notes?state=Trashed")).TotalCount, Is.EqualTo(1));

        var restored = await user.PostActionAsync(note.Id, "restore");
        Assert.That(restored.Note.State, Is.EqualTo(NoteState.Active));
    }

    [Test]
    public async Task DeletePermanently_OnlyFromTrash_RemovesNoteAndVersions()
    {
        var user = await factory.CreateUserAsync();
        var note = (await user.CreateNoteAsync("a:A")).Note;
        await user.UpdateNoteAsync(note, 1, "a:A1");

        await Api.AssertProblemAsync(await user.Http.DeleteAsync($"/api/v1/notes/{note.Id}"), HttpStatusCode.BadRequest,
            ErrorCodes.ValidationFailed, NotesErrorCodes.NoteNotInTrash);

        await user.PostActionAsync(note.Id, "trash");
        var delete = await user.Http.DeleteAsync($"/api/v1/notes/{note.Id}");

        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await Api.AssertProblemAsync(await user.Http.GetAsync($"/api/v1/notes/{note.Id}"), HttpStatusCode.NotFound, ErrorCodes.NotFound);
        Assert.That(await VersionCountAsync(user.UserId, note.Id), Is.Zero);
    }

    [Test]
    public async Task DeleteCategory_WithNotes_MovesNotesToDefaultCategory()
    {
        var user = await factory.CreateUserAsync();
        var work = await user.CreateCategoryAsync("Práce");
        var note = (await user.CreateNoteAsync("a:A")).Note;
        var moved = await Api.ReadAsync<NoteWriteResponse>(await user.PutNoteAsync(note, 1, note.Document, categoryId: work.Id), HttpStatusCode.OK);
        Assert.That(moved.Note.CategoryId, Is.EqualTo(work.Id));

        var delete = await user.Http.DeleteAsync($"/api/v1/categories/{work.Id}");

        Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var reloaded = await user.GetNoteAsync(note.Id);
        Assert.That(reloaded.CategoryId, Is.EqualTo((await user.DefaultCategoryAsync()).Id));
        Assert.That(Api.Text(reloaded), Is.EqualTo("a:A"));
    }

    [Test]
    public async Task DeleteCategory_Default_IsRejected()
    {
        var user = await factory.CreateUserAsync();
        var defaultCategory = await user.DefaultCategoryAsync();

        await Api.AssertProblemAsync(await user.Http.DeleteAsync($"/api/v1/categories/{defaultCategory.Id}"), HttpStatusCode.BadRequest,
            ErrorCodes.ValidationFailed, NotesErrorCodes.DefaultCategoryCannotBeDeleted);
    }

    [Test]
    public async Task Tags_AcrossCategories_FilterAndDelete()
    {
        var user = await factory.CreateUserAsync();
        var tag = await user.CreateTagAsync("důležité");
        var work = await user.CreateCategoryAsync("Práce");
        var first = (await user.CreateNoteAsync("a:A")).Note;
        var second = (await user.CreateNoteAsync("a:B")).Note;
        await Api.ReadAsync<NoteWriteResponse>(await user.PutNoteAsync(first, 1, first.Document, tagIds: [tag.Id]), HttpStatusCode.OK);
        await Api.ReadAsync<NoteWriteResponse>(await user.PutNoteAsync(second, 1, second.Document, tagIds: [tag.Id], categoryId: work.Id), HttpStatusCode.OK);

        var tagged = await user.GetAsync<PagedResponse<NoteDto>>($"/api/v1/notes?tagId={tag.Id}");
        Assert.That(tagged.TotalCount, Is.EqualTo(2));

        Assert.That((await user.Http.DeleteAsync($"/api/v1/tags/{tag.Id}")).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That((await user.GetNoteAsync(first.Id)).TagIds, Is.Empty);
        Assert.That((await user.GetNoteAsync(second.Id)).TagIds, Is.Empty);
    }

    [Test]
    public async Task List_PinnedAndPaging_PinnedFirstAndTotalCount()
    {
        var user = await factory.CreateUserAsync();
        var first = (await user.CreateNoteAsync("a:1")).Note;
        await user.CreateNoteAsync("a:2");
        await user.CreateNoteAsync("a:3");
        await Api.ReadAsync<NoteWriteResponse>(await user.PutNoteAsync(first, 1, first.Document, pinned: true), HttpStatusCode.OK);

        var page = await user.GetAsync<PagedResponse<NoteDto>>("/api/v1/notes?page=1&pageSize=2");

        Assert.That(page.TotalCount, Is.EqualTo(3));
        Assert.That(page.Items, Has.Count.EqualTo(2));
        Assert.That(page.Items[0].Id, Is.EqualTo(first.Id));
    }

    [Test]
    public async Task Create_InvalidDocument_IsValidationProblem()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Http.PostAsJsonAsync("/api/v1/notes",
            new CreateNoteRequest(Api.NewId(), System.Text.Json.JsonSerializer.SerializeToElement(new { type = "doc" })), Api.Json);

        await Api.AssertProblemAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed, "document_invalid");
    }

    [Test]
    public async Task Create_BodyWithInvalidUtf8_IsValidationProblem()
    {
        var user = await factory.CreateUserAsync();
        var json = "{\"id\":\"" + Api.NewId() + """
            ","document":{"type":"doc","schemaVersion":1,"content":[{"type":"paragraph","attrs":{"id":"a"},"content":[{"type":"text","text":"Certifik_t"}]}]}}
            """;
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        bytes[Array.IndexOf(bytes, (byte)'_')] = 0xE1; // „á“ v kódování Windows-1250, neplatné UTF-8

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        var response = await user.Http.PostAsync("/api/v1/notes", content);

        Assert.That((int)response.StatusCode, Is.LessThan(500), await response.Content.ReadAsStringAsync());
    }

    [Test]
    public async Task Update_UnknownCategory_IsValidationProblem()
    {
        var user = await factory.CreateUserAsync();
        var note = (await user.CreateNoteAsync("a:A")).Note;

        var response = await user.PutNoteAsync(note, 1, note.Document, categoryId: Api.NewId());

        await Api.AssertProblemAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed, "category_not_found");
    }

    private async Task<int> VersionCountAsync(Guid userId, Guid noteId) =>
        await TestUsers.AsUserAsync(factory.Services, userId, services =>
            services.GetRequiredService<CrumaDbContext>().Set<NoteVersionEntity>().CountAsync(version => version.NoteId == noteId));
}
