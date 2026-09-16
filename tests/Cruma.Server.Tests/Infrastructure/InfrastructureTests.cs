using System.Net;
using System.Text.Json;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes;
using Cruma.Server.Notes.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Npgsql;

namespace Cruma.Server.Tests.Infrastructure;

/// <summary>Serverová infrastruktura (T-21): filtr vlastníka, ProblemDetails, correlation id, allowlist, migrace.</summary>
public class InfrastructureTests
{
    private CrumaServerFactory factory = null!;

    [OneTimeSetUp]
    public void StartServer() => factory = new CrumaServerFactory();

    [OneTimeTearDown]
    public void StopServer() => factory.Dispose();

    [Test]
    public async Task OwnerFilter_RowsOfOtherUser_AreInvisible()
    {
        var alice = await factory.CreateUserAsync();
        var bob = await factory.CreateUserAsync();
        var note = (await alice.CreateNoteAsync("a:tajné")).Note;

        var seenByBob = await TestUsers.AsUserAsync(factory.Services, bob.UserId, services =>
            services.GetRequiredService<CrumaDbContext>().Set<NoteEntity>().AnyAsync(entity => entity.Id == note.Id));
        var seenWithoutUser = await TestUsers.AsUserAsync(factory.Services, null, services =>
            services.GetRequiredService<CrumaDbContext>().Set<NoteEntity>().AnyAsync(entity => entity.Id == note.Id));
        var seenByAlice = await TestUsers.AsUserAsync(factory.Services, alice.UserId, services =>
            services.GetRequiredService<CrumaDbContext>().Set<NoteEntity>().AnyAsync(entity => entity.Id == note.Id));

        Assert.That(seenByBob, Is.False);
        Assert.That(seenWithoutUser, Is.False);
        Assert.That(seenByAlice, Is.True);
    }

    [Test]
    public async Task NamedBypass_AcrossAllUsers_SeesRowsOfOtherUser()
    {
        var alice = await factory.CreateUserAsync();
        var bob = await factory.CreateUserAsync();
        var note = (await alice.CreateNoteAsync("a:A")).Note;

        var seen = await TestUsers.AsUserAsync(factory.Services, bob.UserId, services =>
            services.GetRequiredService<CrumaDbContext>().AcrossAllUsers<NoteEntity>("test obejití").AnyAsync(entity => entity.Id == note.Id));

        Assert.That(seen, Is.True);
    }

    [Test]
    public async Task SaveChanges_EntityOfOtherUser_Throws()
    {
        var alice = await factory.CreateUserAsync();
        var bob = await factory.CreateUserAsync();

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => TestUsers.AsUserAsync(factory.Services, bob.UserId, async services =>
        {
            var db = services.GetRequiredService<CrumaDbContext>();
            db.Set<TagEntity>().Add(new TagEntity { Id = Api.NewId(), OwnerUserId = alice.UserId, Name = "cizí" });
            return await db.SaveChangesAsync();
        }));

        Assert.That(exception!.Message, Does.Contain("PER-002"));
    }

    [Test]
    public void SaveChanges_UserDataWithoutUser_Throws()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => TestUsers.AsUserAsync(factory.Services, null, async services =>
        {
            var db = services.GetRequiredService<CrumaDbContext>();
            db.Set<TagEntity>().Add(new TagEntity { Id = Api.NewId(), Name = "bez uživatele" });
            return await db.SaveChangesAsync();
        }));
    }

    [Test]
    public async Task Problem_NotFound_HasCodeAndEchoesCorrelationId()
    {
        var user = await factory.CreateUserAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/notes/{Api.NewId()}");
        request.Headers.Add(RequestContext.CorrelationHeader, "test-correlation-1");

        var response = await user.Http.SendAsync(request);

        var problem = await Api.AssertProblemAsync(response, HttpStatusCode.NotFound, ErrorCodes.NotFound);
        Assert.That(problem.GetProperty("correlationId").GetString(), Is.EqualTo("test-correlation-1"));
        Assert.That(response.Headers.GetValues(RequestContext.CorrelationHeader), Is.EqualTo(new[] { "test-correlation-1" }));
    }

    [Test]
    public async Task Problem_UnauthenticatedApiRequest_IsUnauthenticated()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/notes");

        await Api.AssertProblemAsync(response, HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [Test]
    public async Task Problem_UnexpectedException_IsGenericInternalError()
    {
        var notes = new Mock<INotesService>();
        notes.Setup(service => service.ListNotesAsync(It.IsAny<NoteListQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("vnitřní detail, který nesmí ven"));
        using var failing = new CrumaServerFactory().WithServices(services =>
        {
            services.RemoveAll<INotesService>();
            services.AddScoped(_ => notes.Object);
        });
        var user = await failing.CreateUserAsync();

        var response = await user.Http.GetAsync("/api/v1/notes");

        var problem = await Api.AssertProblemAsync(response, HttpStatusCode.InternalServerError, ErrorCodes.InternalError);
        Assert.That(problem.GetRawText(), Does.Not.Contain("vnitřní detail"));
        Assert.That(problem.TryGetProperty("correlationId", out _), Is.True);
    }

    [Test]
    public async Task Health_WithoutSignIn_IsAvailable()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void Endpoints_AnonymousAccess_OnlyOnAllowlist()
    {
        using var scope = factory.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

        var anonymous = endpoints
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(endpoint => endpoint.RoutePattern.RawText!)
            .Distinct()
            .ToList();

        Assert.That(endpoints, Has.Count.GreaterThan(10));
        Assert.That(anonymous, Is.SubsetOf(InfrastructureModule.AnonymousAllowlist), "API-003: anonymní endpoint mimo allowlist");
    }

    [Test]
    public async Task Startup_EmptyDatabase_DoesNotApplyMigrations()
    {
        var connectionString = await TestDatabase.CreateEmptyDatabaseAsync("cruma_no_migrations");
        using var server = new CrumaServerFactory(connectionString);

        var health = await server.CreateClient().GetAsync("/health");

        Assert.That(health.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("select count(*) from information_schema.schemata where schema_name = 'notes'", connection);
        Assert.That((long)(await command.ExecuteScalarAsync())!, Is.Zero, "PER-003: server při startu migrace neaplikuje");
    }

    [Test]
    public async Task Json_Enums_AreSerializedAsText()
    {
        var user = await factory.CreateUserAsync();
        var note = await user.CreateNoteAsync("a:A");

        var raw = await user.Http.GetStringAsync($"/api/v1/notes/{note.Note.Id}");

        Assert.That(JsonDocument.Parse(raw).RootElement.GetProperty("state").GetString(), Is.EqualTo("Active"));
    }
}
