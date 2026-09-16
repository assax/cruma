using System.Net.Http.Json;
using Cruma.Api.Contracts;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Cruma.Server.Tests.Notes;

/// <summary>Spojování uložení do verzí (T-25, plan.md N-3, FR-6 akc. 3, VER-006).</summary>
public class VersionCoalescingTests
{
    private static readonly Guid Phone = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Laptop = Guid.Parse("11111111-0000-0000-0000-000000000002");

    private FakeTimeProvider clock = null!;
    private CrumaServerFactory factory = null!;

    [SetUp]
    public void StartServer()
    {
        clock = new FakeTimeProvider(new DateTimeOffset(2030, 3, 1, 9, 0, 0, TimeSpan.Zero));
        factory = new CrumaServerFactory { TimeProvider = clock };
    }

    [TearDown]
    public void StopServer() => factory.Dispose();

    [Test]
    public async Task Save_SameClientWithinInterval_UpdatesLastVersion()
    {
        var phone = await factory.CreateUserAsync(Phone);
        var note = (await phone.CreateNoteAsync("a:A")).Note;

        clock.Advance(TimeSpan.FromMinutes(2));
        var first = await phone.UpdateNoteAsync(note, 1, "a:A1");
        clock.Advance(TimeSpan.FromMinutes(4));
        var second = await phone.UpdateNoteAsync(first.Note, first.Note.Version, "a:A12");

        Assert.That(first.Note.Version, Is.EqualTo(1));
        Assert.That(second.Note.Version, Is.EqualTo(1));
        Assert.That(Api.Text(second.Note), Is.EqualTo("a:A12"));
        Assert.That(await VersionCountAsync(phone.UserId, note.Id), Is.EqualTo(1));
    }

    [Test]
    public async Task Save_AfterInactivityInterval_CreatesNewVersion()
    {
        var phone = await factory.CreateUserAsync(Phone);
        var note = (await phone.CreateNoteAsync("a:A")).Note;

        clock.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(1));
        var saved = await phone.UpdateNoteAsync(note, 1, "a:A1");

        Assert.That(saved.Note.Version, Is.EqualTo(2));
    }

    [Test]
    public async Task Save_ChangeFromOtherClientInBetween_IsNotCoalesced()
    {
        var phone = await factory.CreateUserAsync(Phone);
        var laptop = factory.CreateClientFor(phone.UserId, Laptop);
        var note = (await phone.CreateNoteAsync("a:A", "b:B")).Note;

        var fromLaptop = await laptop.UpdateNoteAsync(note, 1, "a:A", "b:B-laptop");
        var fromPhone = await phone.UpdateNoteAsync(fromLaptop.Note, fromLaptop.Note.Version, "a:A-phone", "b:B-laptop");

        Assert.That(fromLaptop.Note.Version, Is.EqualTo(2));
        Assert.That(fromPhone.Note.Version, Is.EqualTo(3));
    }

    [Test]
    public async Task Save_WithoutClientInstance_AlwaysCreatesVersion()
    {
        var anonymousClient = await factory.CreateUserAsync();
        var note = (await anonymousClient.CreateNoteAsync("a:A")).Note;

        var saved = await anonymousClient.UpdateNoteAsync(note, 1, "a:A1");

        Assert.That(saved.Note.Version, Is.EqualTo(2));
    }

    [Test]
    public async Task Save_ConfiguredInterval_IsRespected()
    {
        using var shortInterval = new CrumaServerFactory { TimeProvider = clock }.WithSetting("Cruma:Notes:VersionCoalescingInterval", "00:00:30");
        var phone = await shortInterval.CreateUserAsync(Phone);
        var note = (await phone.CreateNoteAsync("a:A")).Note;

        clock.Advance(TimeSpan.FromMinutes(1));
        var saved = await phone.UpdateNoteAsync(note, 1, "a:A1");

        Assert.That(saved.Note.Version, Is.EqualTo(2));
    }

    [Test]
    public async Task ConflictResolution_IsNeverCoalesced()
    {
        var phone = await factory.CreateUserAsync(Phone);
        var laptop = factory.CreateClientFor(phone.UserId, Laptop);
        var note = (await phone.CreateNoteAsync("a:A")).Note;
        clock.Advance(TimeSpan.FromMinutes(10));
        await laptop.UpdateNoteAsync(note, 1, "a:A-laptop");
        var conflicted = await phone.UpdateNoteAsync(note, 1, "a:A-phone");

        var resolved = await Api.ReadAsync<NoteWriteResponse>(
            await phone.Http.PostAsJsonAsync($"/api/v1/notes/{note.Id}/conflicts/a/resolve", new ResolveConflictRequest(conflicted.Note.Version, "current", null), Api.Json),
            System.Net.HttpStatusCode.OK);

        Assert.That(conflicted.Outcome, Is.EqualTo("conflict"));
        Assert.That(resolved.Note.Version, Is.EqualTo(conflicted.Note.Version + 1));
    }

    private async Task<int> VersionCountAsync(Guid userId, Guid noteId) =>
        await TestUsers.AsUserAsync(factory.Services, userId, services =>
            services.GetRequiredService<CrumaDbContext>().Set<NoteVersionEntity>().CountAsync(version => version.NoteId == noteId));
}
