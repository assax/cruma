using Cruma.Server.Audit;
using Cruma.Server.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;

namespace Cruma.Server.Tests.Audit;

/// <summary>Modul auditu (T-23): tvar záznamu bez obsahu, dotazy, append-only (FR-35, AUD-001..AUD-004).</summary>
public class AuditTests
{
    private CrumaServerFactory factory = null!;

    [OneTimeSetUp]
    public void StartServer() => factory = new CrumaServerFactory();

    [OneTimeTearDown]
    public void StopServer() => factory.Dispose();

    [Test]
    public async Task NoteLifecycle_EmitsEventsWithoutContent()
    {
        var user = await factory.CreateUserAsync();
        var note = (await user.CreateNoteAsync("a:velmi tajný obsah")).Note;
        await Api.ReadAsync<Cruma.Api.Contracts.NoteWriteResponse>(
            await user.PutNoteAsync(note, 1, Api.Doc("a:jiný tajný obsah"), title: "Tajný název"), System.Net.HttpStatusCode.OK);
        await user.PostActionAsync(note.Id, "archive");
        await user.PostActionAsync(note.Id, "trash");
        await user.PostActionAsync(note.Id, "restore");
        await user.Http.PostAsync($"/api/v1/notes/{note.Id}/trash", null);
        await user.Http.DeleteAsync($"/api/v1/notes/{note.Id}");

        var records = (await FindAsync(new AuditQuery(user.UserId, null, null, null))).Where(record => record.ObjectId == note.Id.ToString()).ToList();

        Assert.That(records.Select(record => record.OperationType), Is.EqualTo(new[]
        {
            AuditOperations.NoteCreated, AuditOperations.NoteUpdated, AuditOperations.NoteArchived, AuditOperations.NoteTrashed,
            AuditOperations.NoteRestored, AuditOperations.NoteTrashed, AuditOperations.NoteDeletedPermanently,
        }));
        Assert.That(records, Has.All.Matches<AuditRecord>(record =>
            record.UserId == user.UserId && record.ObjectType == AuditObjectTypes.Note && record.ClientType == "web" && record.Result == AuditResult.Success
            && !string.IsNullOrEmpty(record.CorrelationId)));
        var serialized = System.Text.Json.JsonSerializer.Serialize(records);
        Assert.That(serialized, Does.Not.Contain("tajn"), "AUD-002: audit nikdy neobsahuje obsah ani název");
    }

    [Test]
    public async Task Find_ByTimeAndOperation_ReturnsMatchingRecords()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 12, 0, 0, TimeSpan.Zero));
        using var timed = new CrumaServerFactory { TimeProvider = clock };
        var user = await timed.CreateUserAsync();
        await user.CreateNoteAsync("a:A");
        clock.Advance(TimeSpan.FromHours(2));
        await user.CreateNoteAsync("a:B");

        var inWindow = await TestUsers.AsUserAsync(timed.Services, user.UserId, services =>
            services.GetRequiredService<IAuditService>().FindAsync(
                new AuditQuery(user.UserId, clock.GetUtcNow().AddMinutes(-30), clock.GetUtcNow().AddMinutes(1), AuditOperations.NoteCreated),
                CancellationToken.None));

        Assert.That(inWindow, Has.Count.EqualTo(1));
        Assert.That(inWindow[0].OccurredAtUtc, Is.EqualTo(clock.GetUtcNow()));
    }

    [Test]
    public async Task AuditRecords_UpdateOrDelete_IsRejectedByDatabase()
    {
        var user = await factory.CreateUserAsync();
        await user.CreateNoteAsync("a:A");

        await using var connection = new NpgsqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync();
        await using var update = new NpgsqlCommand($"UPDATE audit.audit_events SET result = 'failure' WHERE user_id = '{user.UserId}'", connection);
        await using var delete = new NpgsqlCommand($"DELETE FROM audit.audit_events WHERE user_id = '{user.UserId}'", connection);

        Assert.ThrowsAsync<PostgresException>(() => update.ExecuteNonQueryAsync());
        Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());
    }

    private Task<IReadOnlyList<AuditRecord>> FindAsync(AuditQuery query) =>
        TestUsers.AsUserAsync(factory.Services, query.UserId, services =>
            services.GetRequiredService<IAuditService>().FindAsync(query, CancellationToken.None));
}
