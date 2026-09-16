using Cruma.Server.Infrastructure;
using Cruma.Sync;
using Microsoft.Extensions.Options;

namespace Cruma.Server.Sync;

/// <summary>Synchronizační endpointy desktopu pod <c>/api/sync/v1</c> (server-pattern.md §1, API-001).</summary>
public static class SyncModule
{
    public const string AppVersionHeader = "X-Cruma-App-Version";
    public const string ProtocolVersionHeader = "X-Cruma-Protocol-Version";

    public static IServiceCollection AddSyncModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SyncOptions>(configuration.GetSection(SyncOptions.Section));
        services.AddScoped<IChangeFeed, ChangeFeed>();
        services.AddScoped<SyncService>();
        return services;
    }

    public static void MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var sync = endpoints.MapGroup("/api/sync/v1").AddEndpointFilter(MarkDesktopClient);

        sync.MapPost("/handshake", async (HandshakeRequest request, SyncService service, CancellationToken cancellationToken) =>
        {
            var response = await service.HandshakeAsync(request, cancellationToken);
            return response.Status == HandshakeStatus.Accepted ? Results.Ok(response) : Unsupported(response.MinimumAppVersion);
        });

        var session = sync.MapGroup(string.Empty).AddEndpointFilter(RequireSupportedClient);

        session.MapPost("/push", async (PushRequest request, SyncService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.PushAsync(request, cancellationToken)));

        session.MapPost("/pull", async (PullRequest request, SyncService service, CancellationToken cancellationToken) =>
            TypedResults.Ok(await service.PullAsync(request, cancellationToken)));
    }

    private static ValueTask<object?> MarkDesktopClient(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var requestContext = context.HttpContext.RequestServices.GetRequiredService<RequestContext>();
        requestContext.ClientType = ClientType.Desktop;
        return next(context);
    }

    // Push a pull nesou verzi klienta v hlavičkách; klient pod minimální verzí se odmítne i mimo handshake (SYN-001).
    private static async ValueTask<object?> RequireSupportedClient(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var headers = context.HttpContext.Request.Headers;
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<SyncOptions>>().Value;
        var protocol = int.TryParse(headers[ProtocolVersionHeader].ToString(), out var parsed) ? parsed : 0;
        var instance = context.HttpContext.RequestServices.GetRequiredService<RequestContext>().ClientInstanceId ?? Guid.Empty;

        var response = VersionCompatibility.Evaluate(new HandshakeRequest(headers[AppVersionHeader].ToString(), protocol, instance), options.MinimumVersion);
        return response.Status == HandshakeStatus.Accepted ? await next(context) : Unsupported(response.MinimumAppVersion);
    }

    private static IResult Unsupported(string minimumVersion) =>
        ProblemResults.Problem(new ServiceError(
            ErrorCodes.ClientVersionUnsupported,
            "Verze aplikace není podporovaná, aktualizujte ji.",
            new Dictionary<string, object?> { ["minimumVersion"] = minimumVersion }));
}
