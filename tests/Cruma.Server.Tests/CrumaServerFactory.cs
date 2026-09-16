using System.Security.Claims;
using System.Text.Encodings.Web;
using Cruma.Server.Identity;
using Cruma.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cruma.Server.Tests;

/// <summary>
/// Server v procesu nad testovací databází. Ve výchozím režimu se přihlašuje testovacím schématem podle hlavičky
/// <c>X-Test-User</c>; s <see cref="UseRealAuthentication"/> běží skutečná cookie session.
/// </summary>
public sealed class CrumaServerFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> settings = new();
    private readonly List<Action<IServiceCollection>> serviceOverrides = [];

    public CrumaServerFactory(string? connectionString = null)
    {
        settings[InfrastructureModule.ConnectionStringKey] = connectionString ?? TestDatabase.ConnectionString;
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    public string Environment { get; init; } = "Testing";

    public bool UseRealAuthentication { get; init; }

    public TimeProvider? TimeProvider { get; init; }

    public CrumaServerFactory WithSetting(string key, string? value)
    {
        settings[key] = value;
        return this;
    }

    public CrumaServerFactory WithServices(Action<IServiceCollection> configure)
    {
        serviceOverrides.Add(configure);
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        // Testy technické logy nepotřebují; výpis do konzole by zkresloval i výkonový scénář.
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            if (!UseRealAuthentication)
            {
                services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            }

            if (TimeProvider is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(TimeProvider);
            }

            foreach (var configure in serviceOverrides)
            {
                configure(services);
            }
        });
    }

    /// <summary>Založí nového uživatele jako při prvním přihlášení a vrátí klienta přihlášeného za něj.</summary>
    public async Task<UserClient> CreateUserAsync(Guid? clientInstanceId = null)
    {
        var userId = await TestUsers.SignInAsync(Services);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId.ToString());
        if (clientInstanceId is { } instance)
        {
            client.DefaultRequestHeaders.Add(RequestContext.ClientInstanceHeader, instance.ToString());
        }

        return new UserClient(userId, client);
    }

    /// <summary>Klient téhož uživatele s jinou instancí klienta (jiné zařízení nebo záložka).</summary>
    public UserClient CreateClientFor(Guid userId, Guid? clientInstanceId = null, string? clientType = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId.ToString());
        if (clientInstanceId is { } instance)
        {
            client.DefaultRequestHeaders.Add(RequestContext.ClientInstanceHeader, instance.ToString());
        }

        if (clientType is not null)
        {
            client.DefaultRequestHeaders.Add(RequestContext.ClientHeader, clientType);
        }

        return new UserClient(userId, client);
    }
}

/// <summary>Klient API přihlášený za konkrétního uživatele.</summary>
public sealed record UserClient(Guid UserId, HttpClient Http);

/// <summary>Testovací autentizace: identifikátor uživatele z hlavičky; bez hlavičky nepřihlášený požadavek.</summary>
internal sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers[UserHeader].ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity([new Claim(CrumaClaims.UserId, userId.ToString())], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

/// <summary>Zakládání testovacích uživatelů přes modul identity – deterministické subjekty v rámci běhu.</summary>
public static class TestUsers
{
    private static int counter;

    public static async Task<Guid> SignInAsync(IServiceProvider services, string? subject = null)
    {
        await using var scope = services.CreateAsyncScope();
        var identity = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        return await identity.SignInExternalAsync(
            new ExternalLogin("test", subject ?? $"user-{Interlocked.Increment(ref counter)}"),
            CancellationToken.None);
    }

    /// <summary>Spustí kód v rozsahu služeb jménem uživatele (pro přímé testy služeb a databáze).</summary>
    public static async Task<T> AsUserAsync<T>(IServiceProvider services, Guid? userId, Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = services.CreateAsyncScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
        using var acting = userId is { } id ? currentUser.ActAs(id) : null;
        return await action(scope.ServiceProvider);
    }

    public sealed class NoUser : ICurrentUser
    {
        public Guid? UserId => null;
    }
}
