using System.Net;
using System.Net.Http.Json;
using Cruma.Api.Contracts;
using Cruma.Server.Audit;
using Cruma.Server.Identity;
using Cruma.Server.Infrastructure;
using Cruma.Server.Notes.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cruma.Server.Tests.Identity;

/// <summary>Modul identity (T-22): založení uživatele (N-4), cookie session, odhlášení, audit přihlášení.</summary>
public class IdentityTests
{
    private CrumaServerFactory factory = null!;

    [OneTimeSetUp]
    public void StartServer() => factory = new CrumaServerFactory();

    [OneTimeTearDown]
    public void StopServer() => factory.Dispose();

    [Test]
    public async Task SignInExternal_FirstAndSecondTime_CreatesUserWithOneDefaultCategory()
    {
        var first = await TestUsers.SignInAsync(factory.Services, "identity-first-login");
        var second = await TestUsers.SignInAsync(factory.Services, "identity-first-login");

        var categories = await TestUsers.AsUserAsync(factory.Services, first, services =>
            services.GetRequiredService<CrumaDbContext>().Set<CategoryEntity>().ToListAsync());
        var audit = await FindAuditAsync(first);

        Assert.That(second, Is.EqualTo(first));
        Assert.That(categories, Has.Count.EqualTo(1));
        Assert.That(categories[0].IsDefault, Is.True);
        Assert.That(audit.Count(record => record.OperationType == AuditOperations.ProviderLinked), Is.EqualTo(1));
        Assert.That(audit.Count(record => record.OperationType == AuditOperations.SignIn), Is.EqualTo(2));
    }

    [Test]
    public async Task SignInExternal_SameSubjectAtOtherProvider_IsOtherUser()
    {
        using var scope = factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();

        var google = await identity.SignInExternalAsync(new ExternalLogin("google", "identity-shared-subject"), CancellationToken.None);
        var other = await identity.SignInExternalAsync(new ExternalLogin("microsoft", "identity-shared-subject"), CancellationToken.None);

        Assert.That(other, Is.Not.EqualTo(google));
    }

    [Test]
    public async Task RecordSignInFailed_IsAuditedWithoutUser()
    {
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IIdentityService>().RecordSignInFailedAsync("google", "identity-test-failure", CancellationToken.None);
        }

        var records = await TestUsers.AsUserAsync(factory.Services, null, services =>
            services.GetRequiredService<IAuditService>().FindAsync(new AuditQuery(null, null, null, AuditOperations.SignInFailed), CancellationToken.None));

        Assert.That(records.Any(record => record.Attributes.GetValueOrDefault("reason") == "identity-test-failure" && record.Result == AuditResult.Failure), Is.True);
    }

    [Test]
    public async Task DevelopmentSignIn_InDevelopment_CreatesCookieSessionAndSignsOut()
    {
        using var development = new CrumaServerFactory { Environment = "Development", UseRealAuthentication = true }
            .WithSetting("Cruma:Identity:DevelopmentSignIn", "true");
        var client = development.CreateClient();

        var signIn = await client.PostAsJsonAsync("/auth/dev/sign-in", new DevelopmentSignInRequest("identity-dev-user"));
        var signedIn = await Api.ReadAsync<CurrentUserDto>(signIn, HttpStatusCode.OK);
        var cookie = signIn.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("__Host-cruma=", StringComparison.Ordinal));
        var me = await Api.ReadAsync<CurrentUserDto>(await client.GetAsync("/auth/me"), HttpStatusCode.OK);

        Assert.That(cookie, Does.Contain("httponly").IgnoreCase.And.Contain("secure").IgnoreCase.And.Contain("samesite=lax").IgnoreCase);
        Assert.That(me.UserId, Is.EqualTo(signedIn.UserId));
        Assert.That((await client.GetAsync("/api/v1/notes")).StatusCode, Is.EqualTo(HttpStatusCode.OK));

        Assert.That((await client.PostAsync("/auth/sign-out", null)).StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        await Api.AssertProblemAsync(await client.GetAsync("/auth/me"), HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
        Assert.That((await FindAuditAsync(signedIn.UserId)).Select(record => record.OperationType), Does.Contain(AuditOperations.SignOut));
    }

    [Test]
    public async Task DevelopmentSignIn_OutsideDevelopment_IsNotAvailable()
    {
        using var testing = new CrumaServerFactory { UseRealAuthentication = true }.WithSetting("Cruma:Identity:DevelopmentSignIn", "true");

        var response = await testing.CreateClient().PostAsJsonAsync("/auth/dev/sign-in", new DevelopmentSignInRequest("identity-not-allowed"));

        await Api.AssertProblemAsync(response, HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    [Test]
    public async Task SignInWithProvider_NotConfigured_IsNotFound()
    {
        var response = await factory.CreateClient().GetAsync("/auth/sign-in/google");

        await Api.AssertProblemAsync(response, HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    private Task<IReadOnlyList<AuditRecord>> FindAuditAsync(Guid userId) =>
        TestUsers.AsUserAsync(factory.Services, userId, services =>
            services.GetRequiredService<IAuditService>().FindAsync(new AuditQuery(userId, null, null, null), CancellationToken.None));
}
