using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Cruma.Server.Infrastructure;

/// <summary>Registrace infrastruktury serveru: databáze, aktuální uživatel, kontext požadavku, ProblemDetails.</summary>
public static class InfrastructureModule
{
    /// <summary>Konfigurační klíč připojení k PostgreSQL (env <c>CRUMA__DATABASE__CONNECTIONSTRING</c>).</summary>
    public const string ConnectionStringKey = "Cruma:Database:ConnectionString";

    /// <summary>
    /// Endpointy dostupné bez přihlášení (API-003). Architektonický test hlídá, že žádný jiný endpoint anonymní není.
    /// Mimo endpointy jsou anonymní jen statické soubory: klient Cruma.Web a feed desktopu pod <c>/desktop/</c> (N-6).
    /// </summary>
    public static readonly IReadOnlySet<string> AnonymousAllowlist = new HashSet<string>(StringComparer.Ordinal)
    {
        "/health",
        "/auth/sign-in/{provider}",
        "/auth/providers",
        "/auth/callback",
        "/auth/dev/sign-in",
        "{*path:nonfile}",
    };

    public static IServiceCollection AddInfrastructureModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<CurrentUser>());
        services.AddScoped<RequestContext>();
        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<CrumaDbContext>(options => ConfigureDbContext(options, configuration[ConnectionStringKey]));

        services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemResults.Customize);
        services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Všechno vyžaduje přihlášení, kromě endpointů výslovně označených jako anonymní (API-003, SEC-005).
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    public static void ConfigureDbContext(DbContextOptionsBuilder options, string? connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention();

    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app) => app.UseMiddleware<RequestContextMiddleware>();

    public static void MapInfrastructureEndpoints(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/health", () => TypedResults.Ok(new { status = "ok" })).AllowAnonymous();
}
