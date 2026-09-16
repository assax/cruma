using System.Text.RegularExpressions;

namespace Cruma.Server.Infrastructure;

/// <summary>Typ klienta pro audit a verze (versioning-and-sync-pattern.md §2.2).</summary>
public enum ClientType
{
    Web,
    Desktop,
    Mobile,
}

/// <summary>
/// Kontext požadavku: correlation id (propaguje se do logů, ProblemDetails a auditu) a klient, ze kterého
/// požadavek přišel (typ a identifikátor instance pro spojování uložení – plan.md N-3).
/// </summary>
public sealed partial class RequestContext
{
    public const string CorrelationHeader = "X-Correlation-Id";
    public const string ClientHeader = "X-Cruma-Client";
    public const string ClientInstanceHeader = "X-Cruma-Client-Instance";

    public string CorrelationId { get; private set; } = string.Empty;

    public ClientType ClientType { get; set; } = ClientType.Web;

    public Guid? ClientInstanceId { get; set; }

    internal void Initialize(HttpContext context)
    {
        var requested = context.Request.Headers[CorrelationHeader].ToString();
        CorrelationId = SafeCorrelationId().IsMatch(requested) ? requested : context.TraceIdentifier;

        if (Enum.TryParse<ClientType>(context.Request.Headers[ClientHeader].ToString(), ignoreCase: true, out var clientType))
        {
            ClientType = clientType;
        }

        if (Guid.TryParse(context.Request.Headers[ClientInstanceHeader].ToString(), out var instance))
        {
            ClientInstanceId = instance;
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]{1,64}$")]
    private static partial Regex SafeCorrelationId();
}

/// <summary>Middleware, které naplní <see cref="RequestContext"/> a otevře logovací scope s correlation id.</summary>
public sealed class RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, RequestContext requestContext)
    {
        requestContext.Initialize(context);
        context.Response.Headers[RequestContext.CorrelationHeader] = requestContext.CorrelationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = requestContext.CorrelationId }))
        {
            await next(context);
        }
    }
}
