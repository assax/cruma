using System.Net;
using Cruma.Api.Client;
using Cruma.Api.Contracts;
using Cruma.Notes;
using Cruma.Server.Infrastructure;

namespace Cruma.Server.Tests.ApiClient;

/// <summary>Typovaný API klient proti skutečnému serveru (T-37): pokrytí endpointů a převod ProblemDetails.</summary>
public class CrumaApiClientTests
{
    private CrumaServerFactory factory = null!;

    [OneTimeSetUp]
    public void StartServer() => factory = new CrumaServerFactory();

    [OneTimeTearDown]
    public void StopServer() => factory.Dispose();

    [Test]
    public async Task NoteLifecycle_ThroughClient_Works()
    {
        var user = await factory.CreateUserAsync();
        var client = new CrumaApiClient(user.Http);
        var id = Api.NewId();

        var created = await client.CreateNoteAsync(new CreateNoteRequest(id, Api.Doc("a:přes klienta")));
        var tag = await client.CreateTagAsync(new CreateTagRequest(Api.NewId(), "štítek"));
        var category = await client.CreateCategoryAsync(new CreateCategoryRequest(Api.NewId(), "Kategorie"));
        var note = created.Value.Note;
        var updated = await client.UpdateNoteAsync(id, new UpdateNoteRequest(note.Version, "Název", category.Value.Id, [tag.Value.Id], "yellow", true, Api.Doc("a:upraveno")));
        var archived = await client.ArchiveNoteAsync(id);
        var list = await client.ListNotesAsync(NoteState.Archived, category.Value.Id, tag.Value.Id, 1, 10);
        var found = await client.SearchAsync("upraven", null, 1, 10);
        var unarchived = await client.UnarchiveNoteAsync(id);
        var trashed = await client.TrashNoteAsync(id);
        var deleted = await client.DeleteNotePermanentlyAsync(id);

        Assert.That(created.IsSuccess && updated.IsSuccess && archived.IsSuccess && unarchived.IsSuccess && trashed.IsSuccess && deleted.IsSuccess, Is.True);
        Assert.That(updated.Value.Note.Title, Is.EqualTo("Název"));
        Assert.That(list.Value.TotalCount, Is.EqualTo(1));
        Assert.That(found.Value.Items.Single().Id, Is.EqualTo(id));
        Assert.That((await client.GetNoteAsync(id)).Error!.Code, Is.EqualTo(ErrorCodes.NotFound));
    }

    [Test]
    public async Task Problem_WithRule_IsMappedToError()
    {
        var user = await factory.CreateUserAsync();
        var client = new CrumaApiClient(user.Http);
        var defaultCategory = (await client.ListCategoriesAsync()).Value.Items.Single(category => category.IsDefault);

        var result = await client.DeleteCategoryAsync(defaultCategory.Id);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo(ErrorCodes.ValidationFailed));
        Assert.That(result.Error.Rule, Is.EqualTo(NotesErrorCodes.DefaultCategoryCannotBeDeleted));
        Assert.That(result.Error.Status, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(result.Error.CorrelationId, Is.Not.Empty);
    }

    [Test]
    public async Task Unauthenticated_IsMappedToCode()
    {
        var client = new CrumaApiClient(factory.CreateClient());

        var result = await client.GetCurrentUserAsync();

        Assert.That(result.Error!.Code, Is.EqualTo(ErrorCodes.Unauthenticated));
    }

    [Test]
    public async Task ServerUnreachable_IsNetworkUnavailable()
    {
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9/") };
        var client = new CrumaApiClient(http);

        var result = await client.ListNotesAsync(NoteState.Active, null, null, 1, 10);

        Assert.That(result.Error!.IsNetworkFailure, Is.True);
    }

    [Test]
    public async Task ConflictResolution_ThroughClient_Works()
    {
        var user = await factory.CreateUserAsync();
        var client = new CrumaApiClient(user.Http);
        var note = (await user.CreateNoteAsync("a:A")).Note;
        await user.UpdateNoteAsync(note, 1, "a:A1");
        var conflicted = await user.UpdateNoteAsync(note, 1, "a:A2");

        var resolved = await client.ResolveConflictAsync(note.Id, "a", new ResolveConflictRequest(conflicted.Note.Version, "incoming", null));

        Assert.That(resolved.Value.Note.HasConflict, Is.False);
    }

    [Test]
    public async Task Providers_WithoutGoogleConfig_AreEmpty()
    {
        var result = await new CrumaApiClient(factory.CreateClient()).GetSignInProvidersAsync();

        Assert.That(result.Value.ExternalProviders, Is.Empty);
        Assert.That(result.Value.DevelopmentSignIn, Is.False);
    }
}
