using System.Net;
using Cruma.Api.Contracts;

namespace Cruma.Server.Tests.Search;

/// <summary>Modul vyhledávání s adaptérem PostgreSQL přes REST (T-28, FR-24).</summary>
public class SearchApiTests
{
    private CrumaServerFactory factory = null!;

    [OneTimeSetUp]
    public void StartServer() => factory = new CrumaServerFactory();

    [OneTimeTearDown]
    public void StopServer() => factory.Dispose();

    [Test]
    public async Task Search_Acceptance2And3_DiacriticsAndPrefix()
    {
        var user = await factory.CreateUserAsync();
        var certificate = (await user.CreateNoteAsync("a:Certifikát serveru vyprší")).Note;
        var inflected = (await user.CreateNoteAsync("a:Obnova certifikátu v pondělí")).Note;
        await user.CreateNoteAsync("a:Nákupní seznam");
        await user.CreateNoteAsync("a:necertifikovaný zdroj");

        var exact = await SearchAsync(user, "certifikat");
        var prefix = await SearchAsync(user, "certif");

        // Každý token dotazu je prefix (SRC-003), proto `certifikat` najde i `certifikátu`.
        Assert.That(exact.Items.Select(note => note.Id), Is.EquivalentTo(new[] { certificate.Id, inflected.Id }));
        Assert.That(prefix.Items.Select(note => note.Id), Is.EquivalentTo(new[] { certificate.Id, inflected.Id }));
    }

    [Test]
    public async Task Search_Title_IsIndexed()
    {
        var user = await factory.CreateUserAsync();
        var note = (await user.CreateNoteAsync("a:obsah")).Note;
        await Api.ReadAsync<NoteWriteResponse>(await user.PutNoteAsync(note, 1, note.Document, title: "Žluťoučký kůň"), HttpStatusCode.OK);

        Assert.That((await SearchAsync(user, "zlutoucky kun")).Items.Select(found => found.Id), Is.EqualTo(new[] { note.Id }));
    }

    [Test]
    public async Task Search_AfterUpdate_UsesNewContent()
    {
        var user = await factory.CreateUserAsync();
        var note = (await user.CreateNoteAsync("a:původní text")).Note;

        await user.UpdateNoteAsync(note, 1, "a:nový obsah");

        Assert.That((await SearchAsync(user, "puvodni")).TotalCount, Is.Zero);
        Assert.That((await SearchAsync(user, "novy")).TotalCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Search_Acceptance6_StateFilter()
    {
        var user = await factory.CreateUserAsync();
        var active = (await user.CreateNoteAsync("a:projekt alfa")).Note;
        var archived = (await user.CreateNoteAsync("a:projekt beta")).Note;
        var trashed = (await user.CreateNoteAsync("a:projekt gama")).Note;
        await user.PostActionAsync(archived.Id, "archive");
        await user.PostActionAsync(trashed.Id, "trash");

        Assert.That((await SearchAsync(user, "projekt", "Active")).Items.Select(note => note.Id), Is.EqualTo(new[] { active.Id }));
        Assert.That((await SearchAsync(user, "projekt", "Archived")).Items.Select(note => note.Id), Is.EqualTo(new[] { archived.Id }));
        Assert.That((await SearchAsync(user, "projekt", "Trashed")).Items.Select(note => note.Id), Is.EqualTo(new[] { trashed.Id }));
        Assert.That((await SearchAsync(user, "projekt")).TotalCount, Is.EqualTo(3));
    }

    [Test]
    public async Task Search_DeletedNote_IsRemovedFromIndex()
    {
        var user = await factory.CreateUserAsync();
        var note = (await user.CreateNoteAsync("a:smazat natrvalo")).Note;
        await user.PostActionAsync(note.Id, "trash");

        await user.Http.DeleteAsync($"/api/v1/notes/{note.Id}");

        Assert.That((await SearchAsync(user, "natrvalo")).TotalCount, Is.Zero);
    }

    [Test]
    public async Task Search_EmptyQuery_ReturnsNothing()
    {
        var user = await factory.CreateUserAsync();
        await user.CreateNoteAsync("a:cokoli");

        Assert.That((await SearchAsync(user, " -- ")).TotalCount, Is.Zero);
    }

    [Test]
    public async Task Search_Paging_ReturnsPageAndTotal()
    {
        var user = await factory.CreateUserAsync();
        for (var index = 0; index < 5; index++)
        {
            await user.CreateNoteAsync($"a:stránkování {index}");
        }

        var page = await user.GetAsync<PagedResponse<NoteDto>>("/api/v1/search?q=strankovani&page=2&pageSize=2");

        Assert.That(page.TotalCount, Is.EqualTo(5));
        Assert.That(page.Items, Has.Count.EqualTo(2));
    }

    private static Task<PagedResponse<NoteDto>> SearchAsync(UserClient user, string query, string? state = null) =>
        user.GetAsync<PagedResponse<NoteDto>>($"/api/v1/search?q={Uri.EscapeDataString(query)}{(state is null ? string.Empty : "&state=" + state)}");
}
