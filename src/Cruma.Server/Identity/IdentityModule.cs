using System.Security.Claims;
using Cruma.Api.Contracts;
using Cruma.Server.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Cruma.Server.Identity;

/// <summary>
/// Identita (C-14): cookie session pro web (SEC-004), přihlášení externím poskytovatelem přes server (SEC-001),
/// odhlášení. Google se zapojí, jen když jsou v konfiguraci jeho přihlašovací údaje (T-19). Přihlášení desktopu
/// tokenem (authorization code + PKCE, SEC-002) čeká na prototyp T-20.
/// </summary>
public static class IdentityModule
{
    public const string SessionScheme = "Cruma.Session";
    public const string ExternalScheme = "Cruma.External";
    public const string GoogleProvider = "google";
    public const string DevelopmentProvider = "development";

    private const string GoogleClientIdKey = "Cruma:Identity:Google:ClientId";
    private const string GoogleClientSecretKey = "Cruma:Identity:Google:ClientSecret";
    private const string DevelopmentSignInKey = "Cruma:Identity:DevelopmentSignIn";

    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddScoped<IIdentityService, IdentityService>();

        var authentication = services
            .AddAuthentication(SessionScheme)
            .AddCookie(SessionScheme, options =>
            {
                options.Cookie.Name = "__Host-cruma";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);

                // API odpovídá kódy a ProblemDetails, ne přesměrováním na přihlašovací stránku (ERR-001).
                options.Events.OnRedirectToLogin = context => WriteProblemAsync(context.HttpContext, ErrorCodes.Unauthenticated);
                options.Events.OnRedirectToAccessDenied = context => WriteProblemAsync(context.HttpContext, ErrorCodes.Forbidden);
            })
            .AddCookie(ExternalScheme, options =>
            {
                options.Cookie.Name = "__Host-cruma-external";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });

        if (!string.IsNullOrEmpty(configuration[GoogleClientIdKey]) && !string.IsNullOrEmpty(configuration[GoogleClientSecretKey]))
        {
            authentication.AddGoogle(GoogleProvider, options =>
            {
                options.ClientId = configuration[GoogleClientIdKey]!;
                options.ClientSecret = configuration[GoogleClientSecretKey]!;
                options.SignInScheme = ExternalScheme;
                options.Events.OnRemoteFailure = async context =>
                {
                    await context.HttpContext.RequestServices.GetRequiredService<IIdentityService>()
                        .RecordSignInFailedAsync(GoogleProvider, "remote_failure", context.HttpContext.RequestAborted);
                    context.Response.Redirect("/?signInFailed=1");
                    context.HandleResponse();
                };
            });
        }

        services.AddOptions<IdentityRuntimeOptions>().Configure(options =>
            options.DevelopmentSignInEnabled = environment.IsDevelopment() && configuration.GetValue<bool>(DevelopmentSignInKey));

        return services;
    }

    public static void MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth");

        auth.MapGet("/sign-in/{provider}", async (string provider, string? returnUrl, IAuthenticationSchemeProvider schemes) =>
        {
            if (provider == DevelopmentProvider || await schemes.GetSchemeAsync(provider) is null)
            {
                return ProblemResults.Problem(ServiceError.NotFound());
            }

            var properties = new AuthenticationProperties { RedirectUri = $"/auth/callback?returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl))}" };
            properties.Items["provider"] = provider;
            return Results.Challenge(properties, [provider]);
        }).AllowAnonymous();

        auth.MapGet("/callback", async (HttpContext context, string? returnUrl, IIdentityService identity) =>
        {
            var external = await context.AuthenticateAsync(ExternalScheme);
            var subject = external.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            string? provider = null;
            external.Properties?.Items.TryGetValue("provider", out provider);
            if (!external.Succeeded || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(provider))
            {
                await identity.RecordSignInFailedAsync(provider ?? "unknown", "external_authentication_failed", context.RequestAborted);
                return ProblemResults.Problem(new ServiceError(ErrorCodes.Unauthenticated, "Přihlášení se nezdařilo."));
            }

            var userId = await identity.SignInExternalAsync(new ExternalLogin(provider, subject), context.RequestAborted);
            await context.SignOutAsync(ExternalScheme);
            await SignInSessionAsync(context, userId);
            return Results.LocalRedirect(SafeReturnUrl(returnUrl));
        }).AllowAnonymous();

        // Vývojové přihlášení bez Googlu – jen v prostředí Development a se zapnutou volbou.
        auth.MapPost("/dev/sign-in", async (HttpContext context, [FromBody] DevelopmentSignInRequest request, IIdentityService identity,
            Microsoft.Extensions.Options.IOptions<IdentityRuntimeOptions> options) =>
        {
            if (!options.Value.DevelopmentSignInEnabled || string.IsNullOrWhiteSpace(request.Subject))
            {
                return ProblemResults.Problem(ServiceError.NotFound());
            }

            var userId = await identity.SignInExternalAsync(new ExternalLogin(DevelopmentProvider, request.Subject.Trim()), context.RequestAborted);
            await SignInSessionAsync(context, userId);
            return Results.Ok(new CurrentUserDto(userId));
        }).AllowAnonymous();

        auth.MapPost("/sign-out", async (HttpContext context, ICurrentUser currentUser, IIdentityService identity) =>
        {
            if (currentUser.UserId is { } userId)
            {
                await identity.RecordSignOutAsync(userId, context.RequestAborted);
            }

            await context.SignOutAsync(SessionScheme);
            return Results.NoContent();
        });

        auth.MapGet("/me", (ICurrentUser currentUser) => Results.Ok(new CurrentUserDto(currentUser.UserId!.Value)));
    }

    private static Task SignInSessionAsync(HttpContext context, Guid userId) =>
        context.SignInAsync(SessionScheme, new ClaimsPrincipal(new ClaimsIdentity([new Claim(CrumaClaims.UserId, userId.ToString())], SessionScheme)));

    private static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal) && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/";

    private static async Task WriteProblemAsync(HttpContext context, string code)
    {
        context.Response.StatusCode = ErrorCodes.StatusFor(code);
        await context.RequestServices.GetRequiredService<IProblemDetailsService>()
            .WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = { Status = context.Response.StatusCode } });
    }
}

public sealed class IdentityRuntimeOptions
{
    public bool DevelopmentSignInEnabled { get; set; }
}
