using System.Net;
using System.Net.Http.Json;
using Cruma.Server.Audit;
using Cruma.Server.Infrastructure;
using Cruma.Sync;
using Microsoft.Extensions.DependencyInjection;

namespace Cruma.Server.Tests.Sync;

/// <summary>Synchronizační endpoint a změnový feed (T-26, T-30; FR-27, FR-37, SYN-001..SYN-003).</summary>
public class SyncTests
{
    private CrumaServerFactory factory = null!;

    [OneTimeSetUp]
    public void StartServer() => factory = new CrumaServerFactory().WithSetting("Cruma:Sync:MinimumClientVersion", "1.2.0");

    [OneTimeTearDown]
    public void StopServer() => factory.Dispose();

    [Test]
    public async Task Handshake_ClientBelowMinimum_IsRejectedWithCodeAndAudited()
    {
        var user = await factory.CreateUserAsync();
        var instance = Guid.Parse("33333333-0000-0000-0000-000000000001");

        var response = await user.Http.PostAsJsonAsync("/api/sync/v1/handshake", new HandshakeRequest("1.1.9", SyncProtocol.CurrentVersion, instance), Api.Json);

        var problem = await Api.AssertProblemAsync(response, HttpStatusCode.UpgradeRequired, ErrorCodes.ClientVersionUnsupported);
        Assert.That(problem.GetProperty("minimumVersion").GetString(), Is.EqualTo("1.2.0"));
        var audit = await FindAuditAsync(user.UserId, AuditOperations.SyncSession);
        Assert.That(audit.Single().Result, Is.EqualTo(AuditResult.Failure));
        Assert.That(audit.Single().ObjectId, Is.EqualTo(instance.ToString()));
        Assert.That(audit.Single().ClientType, Is.EqualTo("desktop"));
    }

    [Test]
    public async Task Handshake_SupportedClient_IsAccepted()
    {
        var user = await factory.CreateUserAsync();

        var response = await user.Http.PostAsJsonAsync("/api/sync/v1/handshake", new HandshakeRequest("1.2.0", SyncProtocol.CurrentVersion, Api.NewId()), Api.Json);

        var accepted = await Api.ReadAsync<HandshakeResponse>(response, HttpStatusCode.OK);
        Assert.That(accepted.Status, Is.EqualTo(HandshakeStatus.Accepted));
    }

    [Test]
    public async Task Push_ClientBelowMinimumInHeaders_IsRejected()
    {
        var user = await factory.CreateUserAsync();
        var old = factory.Desktop(user.UserId, appVersion: "1.0.0");

        var response = await old.PostAsJsonAsync("/api/sync/v1/push", new PushRequest([]), Api.Json);

        await Api.AssertProblemAsync(response, HttpStatusCode.UpgradeRequired, ErrorCodes.ClientVersionUnsupported);
    }

    [Test]
    public async Task Push_NewNoteRetriedWithSameChangeId_IsProcessedOnce()
    {
        var user = await factory.CreateUserAsync();
        var desktop = factory.Desktop(user.UserId, "1.2.0");
        var category = await factory.DefaultCategoryIdAsync(user.UserId);
        var change = SyncClient.NoteUpsert(Api.NewId(), null, category, "a:offline poznámka");

        var first = await desktop.PushAsync(change);
        var retry = await desktop.PushAsync(change);

        Assert.That(first.Results.Single().Outcome, Is.EqualTo(ChangeOutcome.Applied));
        Assert.That(first.Results.Single().Version, Is.EqualTo(1));
        Assert.That(retry.Results.Single(), Is.EqualTo(first.Results.Single()) .Using<ChangeResult>((left, right) =>
            left.ChangeId == right.ChangeId && left.Outcome == right.Outcome && left.Version == right.Version));
        var note = await user.GetNoteAsync(change.EntityId);
        Assert.That(note.Version, Is.EqualTo(1));
        Assert.That(Api.Text(note), Is.EqualTo("a:offline poznámka"));
    }

    [Test]
    public async Task Push_ChangeRetriedAfterUpdate_DoesNotApplyTwice()
    {
        var user = await factory.CreateUserAsync();
        var desktop = factory.Desktop(user.UserId, "1.2.0");
        var note = (await user.CreateNoteAsync("a:A", "b:B")).Note;
        var change = SyncClient.NoteUpsert(Api.NewId(), note.Id, 1, note.CategoryId, "a:A", "b:B-desktop");

        await desktop.PushAsync(change);
        await user.UpdateNoteAsync(await user.GetNoteAsync(note.Id), 2, "a:A-web", "b:B-desktop");
        var retry = await desktop.PushAsync(change);

        Assert.That(retry.Results.Single().Version, Is.EqualTo(2));
        Assert.That((await user.GetNoteAsync(note.Id)).Version, Is.EqualTo(3));
    }

    [Test]
    public async Task Push_ConcurrentWithWeb_MergesOrConflicts()
    {
        var user = await factory.CreateUserAsync();
        var desktop = factory.Desktop(user.UserId, "1.2.0");
        var note = (await user.CreateNoteAsync("a:A", "b:B")).Note;
        await user.UpdateNoteAsync(note, 1, "a:A-web", "b:B");

        var merged = await desktop.PushAsync(SyncClient.NoteUpsert(note.Id, 1, note.CategoryId, "a:A", "b:B-desktop"));
        var conflict = await desktop.PushAsync(SyncClient.NoteUpsert(note.Id, 1, note.CategoryId, "a:A-desktop", "b:B"));

        Assert.That(merged.Results.Single().Outcome, Is.EqualTo(ChangeOutcome.Merged));
        Assert.That(conflict.Results.Single().Outcome, Is.EqualTo(ChangeOutcome.Conflict));
        Assert.That(Api.Text(await user.GetNoteAsync(note.Id)), Is.EqualTo("a:!A-web|A-desktop b:B-desktop"));
    }

    [Test]
    public async Task Push_InvalidChange_IsRejectedAuditedAndNotRemembered()
    {
        var user = await factory.CreateUserAsync();
        var desktop = factory.Desktop(user.UserId, "1.2.0");
        var categoryId = Api.NewId();
        var change = SyncClient.NoteUpsert(Api.NewId(), null, categoryId, "a:A");

        var rejected = await desktop.PushAsync(change);
        var createCategory = new SyncChange(Api.NewId(), SyncEntityType.Category, categoryId, SyncOperation.Upsert, null, SyncClient.ChangedAt,
            Category: new CategoryPayload("Offline kategorie", false));
        var retried = await desktop.PushAsync(createCategory, change);

        Assert.That(rejected.Results.Single().Outcome, Is.EqualTo(ChangeOutcome.Rejected));
        Assert.That(rejected.Results.Single().ErrorCode, Is.EqualTo("category_not_found"));
        Assert.That(retried.Results.Select(result => result.Outcome), Is.EqualTo(new[] { ChangeOutcome.Applied, ChangeOutcome.Applied }));
        Assert.That((await FindAuditAsync(user.UserId, AuditOperations.SyncChangeRejected)).Single().Attributes["changeId"], Is.EqualTo(change.ChangeId.ToString()));
    }

    [Test]
    public async Task Pull_FromCursor_ReturnsLaterChangesWithCurrentState()
    {
        var user = await factory.CreateUserAsync();
        var desktop = factory.Desktop(user.UserId, "1.2.0");
        var initial = await desktop.PullAsync(0);
        Assert.That(initial.Entries.Select(entry => entry.EntityType), Is.EqualTo(new[] { SyncEntityType.Category }), "výchozí kategorie z založení uživatele");

        var note = (await user.CreateNoteAsync("a:A")).Note;
        await user.UpdateNoteAsync(note, 1, "a:A1");
        var other = (await user.CreateNoteAsync("a:smazat")).Note;
        await user.PostActionAsync(other.Id, "trash");
        await user.Http.DeleteAsync($"/api/v1/notes/{other.Id}");

        var pulled = await desktop.PullAsync(initial.NextCursor);

        Assert.That(pulled.Entries.Select(entry => entry.EntityId), Is.EqualTo(new[] { note.Id, other.Id }), "jeden záznam na entitu");
        Assert.That(pulled.Entries[0].Version, Is.EqualTo(2));
        Assert.That(SyncClient.Text(pulled.Entries[0].Note!), Is.EqualTo("a:A1"));
        Assert.That(pulled.Entries[1].Deleted, Is.True);
        Assert.That(pulled.NextCursor, Is.GreaterThan(initial.NextCursor));
        Assert.That((await desktop.PullAsync(pulled.NextCursor)).Entries, Is.Empty);
    }

    [Test]
    public async Task Pull_LimitedPage_ReportsMoreAndContinues()
    {
        var user = await factory.CreateUserAsync();
        var desktop = factory.Desktop(user.UserId, "1.2.0");
        for (var index = 0; index < 5; index++)
        {
            await user.CreateNoteAsync($"a:{index}");
        }

        var first = await desktop.PullAsync(0, max: 3);
        var second = await desktop.PullAsync(first.NextCursor, max: 3);

        Assert.That(first.HasMore, Is.True);
        Assert.That(first.Entries, Has.Count.EqualTo(3));
        Assert.That(second.HasMore, Is.False);
        Assert.That(first.Entries.Concat(second.Entries).Select(entry => entry.Sequence), Is.Ordered.Ascending.And.Unique);
        Assert.That(first.Entries.Count + second.Entries.Count, Is.EqualTo(6));
    }

    [Test]
    public async Task Feed_ParallelWrites_HaveUniqueGaplessSequences()
    {
        var user = await factory.CreateUserAsync();
        var desktop = factory.Desktop(user.UserId, "1.2.0");

        await Task.WhenAll(Enumerable.Range(0, 10).Select(index => user.CreateNoteAsync($"a:souběh {index}")));

        var pulled = await desktop.PullAsync(0, max: 100);
        Assert.That(pulled.Entries.Select(entry => entry.Sequence), Is.EqualTo(Enumerable.Range(1, 11).Select(value => (long)value)));
    }

    private Task<IReadOnlyList<AuditRecord>> FindAuditAsync(Guid userId, string operation) =>
        TestUsers.AsUserAsync(factory.Services, userId, services =>
            services.GetRequiredService<IAuditService>().FindAsync(new AuditQuery(userId, null, null, operation), CancellationToken.None));
}
