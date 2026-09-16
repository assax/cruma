using System.Security.Claims;

namespace Cruma.Server.Infrastructure;

/// <summary>Aktuální uživatel požadavku – jediný zdroj identity pro moduly (server-pattern.md §4).</summary>
public interface ICurrentUser
{
    /// <summary>Interní identifikátor uživatele; <c>null</c>, pokud požadavek není přihlášený.</summary>
    Guid? UserId { get; }
}

/// <summary>Názvy claimů Cruma.</summary>
public static class CrumaClaims
{
    /// <summary>Interní identifikátor uživatele Cruma (SEC-001).</summary>
    public const string UserId = "cruma:user_id";
}

/// <summary>
/// Aktuální uživatel z přihlášení požadavku. Pojmenované přepnutí <see cref="ActAs"/> slouží jen kódu, který jedná
/// za uživatele ještě před vytvořením session (založení uživatele při prvním přihlášení – plan.md N-4).
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private Guid? actingAs;

    public Guid? UserId => actingAs ?? ReadClaim(httpContextAccessor.HttpContext?.User);

    public IDisposable ActAs(Guid userId)
    {
        var previous = actingAs;
        actingAs = userId;
        return new Restore(() => actingAs = previous);
    }

    private static Guid? ReadClaim(ClaimsPrincipal? principal) =>
        principal?.Identity?.IsAuthenticated == true && Guid.TryParse(principal.FindFirstValue(CrumaClaims.UserId), out var id)
            ? id
            : null;

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
