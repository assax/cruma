using System.Net;
using System.Net.Http.Json;
using Cruma.Api.Contracts;
using Cruma.Server.Infrastructure;
using Cruma.Server.Tests.Sync;
using Cruma.Sync;

namespace Cruma.Server.Tests.Isolation;

/// <summary>
/// Izolace uživatelů (T-31, TST-004, FR-31, testing-strategy.md §5): uživatel B nepřečte, nenajde, neupraví ani
/// nesmaže data uživatele A – dostane <c>not_found</c> nebo prázdný výsledek – ani přes synchronizaci.
/// </summary>
public class UserIsolationTests
{
    private CrumaServerFactory factory = null!;
    private UserClient alice = null!;
    private UserClient bob = null!;
    private NoteDto aliceNote = null!;
    private NoteDto aliceConflict = null!;
    private CategoryDto aliceCategory = null!;
    private TagDto aliceTag = null!;

    [OneTimeSetUp]
    public async Task CreateDataOfAliceAsync()
    {
        factory = new CrumaServerFactory();
        alice = await factory.CreateUserAsync();
        bob = await factory.CreateUserAsync();

        aliceCategory = await alice.CreateCategoryAsync("Alicina kategorie");
        aliceTag = await alice.CreateTagAsync("alicin-stitek");
        aliceNote = (await alice.CreateNoteAsync("a:izolace jedinečnéslovo")).Note;

        var conflicted = (await alice.CreateNoteAsync("a:A")).Note;
        await alice.UpdateNoteAsync(conflicted, 1, "a:A1");
        aliceConflict = (await alice.UpdateNoteAsync(conflicted, 1, "a:A2")).Note;
        await bob.CreateNoteAsync("a:bobova poznámka");
    }

    [OneTimeTearDown]
    public void StopServer() => factory.Dispose();

    private static IEnumerable<TestCaseData> NotFoundRequests()
    {
        yield return Case("GET note", (test) => test.bob.Http.GetAsync($"/api/v1/notes/{test.aliceNote.Id}"));
        yield return Case("PUT note", (test) => test.bob.PutNoteAsync(test.aliceNote, 1, Api.Doc("a:přepsáno")));
        yield return Case("archive", (test) => test.bob.Http.PostAsync($"/api/v1/notes/{test.aliceNote.Id}/archive", null));
        yield return Case("unarchive", (test) => test.bob.Http.PostAsync($"/api/v1/notes/{test.aliceNote.Id}/unarchive", null));
        yield return Case("trash", (test) => test.bob.Http.PostAsync($"/api/v1/notes/{test.aliceNote.Id}/trash", null));
        yield return Case("restore", (test) => test.bob.Http.PostAsync($"/api/v1/notes/{test.aliceNote.Id}/restore", null));
        yield return Case("DELETE note", (test) => test.bob.Http.DeleteAsync($"/api/v1/notes/{test.aliceNote.Id}"));
        yield return Case("resolve conflict", (test) => test.bob.Http.PostAsJsonAsync(
            $"/api/v1/notes/{test.aliceConflict.Id}/conflicts/a/resolve", new ResolveConflictRequest(test.aliceConflict.Version, "current", null), Api.Json));
        yield return Case("rename category", (test) => test.bob.Http.PutAsJsonAsync($"/api/v1/categories/{test.aliceCategory.Id}", new RenameRequest("ukradeno"), Api.Json));
        yield return Case("DELETE category", (test) => test.bob.Http.DeleteAsync($"/api/v1/categories/{test.aliceCategory.Id}"));
        yield return Case("rename tag", (test) => test.bob.Http.PutAsJsonAsync($"/api/v1/tags/{test.aliceTag.Id}", new RenameRequest("ukradeno"), Api.Json));
        yield return Case("DELETE tag", (test) => test.bob.Http.DeleteAsync($"/api/v1/tags/{test.aliceTag.Id}"));
    }

    [TestCaseSource(nameof(NotFoundRequests))]
    public async Task Request_OnDataOfOtherUser_IsNotFound(Func<UserIsolationTests, Task<HttpResponseMessage>> request)
    {
        await Api.AssertProblemAsync(await request(this), HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    [Test]
    public async Task Lists_OfOtherUser_DoNotContainData()
    {
        var notes = await bob.GetAsync<PagedResponse<NoteDto>>("/api/v1/notes");
        var archived = await bob.GetAsync<PagedResponse<NoteDto>>("/api/v1/notes?state=Trashed");
        var byCategory = await bob.GetAsync<PagedResponse<NoteDto>>($"/api/v1/notes?categoryId={aliceCategory.Id}");
        var byTag = await bob.GetAsync<PagedResponse<NoteDto>>($"/api/v1/notes?tagId={aliceTag.Id}");
        var categories = await bob.GetAsync<PagedResponse<CategoryDto>>("/api/v1/categories");
        var tags = await bob.GetAsync<PagedResponse<TagDto>>("/api/v1/tags");

        Assert.That(notes.Items.Select(note => note.Id), Has.None.EqualTo(aliceNote.Id).And.None.EqualTo(aliceConflict.Id));
        Assert.That(notes.TotalCount, Is.EqualTo(1));
        Assert.That(archived.TotalCount, Is.Zero);
        Assert.That(byCategory.TotalCount, Is.Zero);
        Assert.That(byTag.TotalCount, Is.Zero);
        Assert.That(categories.Items.Select(category => category.Id), Has.None.EqualTo(aliceCategory.Id));
        Assert.That(tags.TotalCount, Is.Zero);
    }

    [Test]
    public async Task Search_OfOtherUser_FindsNothing()
    {
        var found = await bob.GetAsync<PagedResponse<NoteDto>>("/api/v1/search?q=jedinecneslovo");
        var aliceFound = await alice.GetAsync<PagedResponse<NoteDto>>("/api/v1/search?q=jedinecneslovo");

        Assert.That(found.TotalCount, Is.Zero);
        Assert.That(aliceFound.TotalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Update_WithCategoryOrTagOfOtherUser_IsRejected()
    {
        var own = (await bob.CreateNoteAsync("a:B")).Note;

        await Api.AssertProblemAsync(await bob.PutNoteAsync(own, 1, own.Document, categoryId: aliceCategory.Id),
            HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed, "category_not_found");
        await Api.AssertProblemAsync(await bob.PutNoteAsync(own, 1, own.Document, tagIds: [aliceTag.Id]),
            HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed, "tag_not_found");
    }

    [Test]
    public async Task Create_WithIdOfOtherUsersNote_DoesNotTouchIt()
    {
        var response = await bob.Http.PostAsJsonAsync("/api/v1/notes", new CreateNoteRequest(aliceNote.Id, Api.Doc("a:převzato")), Api.Json);

        await Api.AssertProblemAsync(response, HttpStatusCode.BadRequest, ErrorCodes.ValidationFailed, "note_id_unavailable");
        Assert.That(Api.Text(await alice.GetNoteAsync(aliceNote.Id)), Is.EqualTo("a:izolace jedinečnéslovo"));
    }

    [Test]
    public async Task Sync_OfOtherUser_NeitherPullsNorChangesData()
    {
        var desktop = factory.Desktop(bob.UserId);
        var bobCategory = await factory.DefaultCategoryIdAsync(bob.UserId);

        var pulled = await desktop.PullAsync(0, max: 500);
        var pushed = await desktop.PushAsync(
            SyncClient.NoteUpsert(aliceNote.Id, 1, bobCategory, "a:přepsáno synchronizací"),
            new SyncChange(Api.NewId(), SyncEntityType.Note, aliceNote.Id, SyncOperation.Delete, 1, SyncClient.ChangedAt),
            new SyncChange(Api.NewId(), SyncEntityType.Category, aliceCategory.Id, SyncOperation.Delete, null, SyncClient.ChangedAt),
            new SyncChange(Api.NewId(), SyncEntityType.Tag, aliceTag.Id, SyncOperation.Delete, null, SyncClient.ChangedAt));

        var aliceIds = new[] { aliceNote.Id, aliceConflict.Id, aliceCategory.Id, aliceTag.Id };
        Assert.That(pulled.Entries.Select(entry => entry.EntityId), Has.None.AnyOf(aliceIds));
        Assert.That(pushed.Results.Select(result => result.Outcome), Has.All.EqualTo(ChangeOutcome.Rejected));
        Assert.That(pushed.Results.Select(result => result.ErrorCode), Is.EqualTo(new[] { ErrorCodes.NotFound, ErrorCodes.NotFound, ErrorCodes.NotFound, ErrorCodes.NotFound }));
        Assert.That(Api.Text(await alice.GetNoteAsync(aliceNote.Id)), Is.EqualTo("a:izolace jedinečnéslovo"));
    }

    private static TestCaseData Case(string name, Func<UserIsolationTests, Task<HttpResponseMessage>> request) =>
        new TestCaseData(request).SetName($"Request_OnDataOfOtherUser_IsNotFound({name})");
}
